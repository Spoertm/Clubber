﻿using Clubber.Configuration;
using Clubber.Helpers;
using Clubber.Models;
using Clubber.Models.Responses;
using Clubber.Services;
using CoreHtmlToImage;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageFormat = CoreHtmlToImage.ImageFormat;

namespace Clubber.BackgroundTasks
{
	public class DdNewsPostService : AbstractBackgroundService
	{
		private const int _minimumScore = 930;
		private const string _cachePath = "LeaderboardCache.json";
		private readonly SocketTextChannel? _ddNewsChannel;
		private readonly DiscordSocketClient _client;
		private readonly WebService _webService;
		private readonly IOService _ioService;
		private readonly DatabaseHelper _databaseHelper;
		private readonly StringBuilder _sb = new();

		public DdNewsPostService(DiscordSocketClient client, WebService webService, IOService ioService, DatabaseHelper databaseHelper)
		{
			_ddNewsChannel = client.GetChannel(Config.DdNewsChannelId) as SocketTextChannel;
			if (_ddNewsChannel is null)
				throw new CustomException("DD news channel doesn't exist.");

			_client = client;
			_webService = webService;
			_ioService = ioService;
			_databaseHelper = databaseHelper;
			_ioService.GetLbEntriesCacheFromDiscordAndSaveToFile();
		}

		protected override TimeSpan Interval => TimeSpan.FromMinutes(2);

		protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
		{
			if (_ddNewsChannel is null)
				return;

			List<EntryResponse> oldEntries = await _ioService.GetLbEntriesCacheFromFile();
			List<EntryResponse> newEntries = await GetSufficientLeaderboardEntries();
			(EntryResponse OldEntry, EntryResponse NewEntry)[] entryTuples = oldEntries.Join(
					inner: newEntries,
					outerKeySelector: oldEntry => oldEntry.Id,
					innerKeySelector: newEntry => newEntry.Id,
					resultSelector: (oldEntry, newEntry) => (oldEntry, newEntry))
				.ToArray();

			bool cacheIsToBeRefreshed = newEntries.Count > oldEntries.Count;
			foreach ((EntryResponse oldEntry, EntryResponse newEntry) in entryTuples)
			{
				if (oldEntry.Time == newEntry.Time || newEntry.Time / 10000 < 1000)
					continue;

				cacheIsToBeRefreshed = true;
				await PostDdNews(newEntries, (oldEntry, newEntry));
			}

			if (cacheIsToBeRefreshed)
				await _ioService.UpdateLbEntriesCache(_cachePath, JsonConvert.SerializeObject(newEntries));
		}

		private async Task<List<EntryResponse>> GetSufficientLeaderboardEntries()
		{
			List<EntryResponse> entries = new();
			int rank = 1;
			do
			{
				entries.AddRange((await _webService.GetLeaderboardEntries(rank)).Entries);
				rank += 100;
				await Task.Delay(50);
			}
			while (entries[^1].Time / 10000 > _minimumScore);

			return entries;
		}

		private async Task PostDdNews(List<EntryResponse> newEntries, (EntryResponse OldEntry, EntryResponse NewEntry) entryTuple)
		{
			_sb.Clear().Append("Congratulations to ");
			string userName = entryTuple.NewEntry.Username;
			DdUser? dbUser = _databaseHelper.GetDdUserByLbId(entryTuple.NewEntry.Id);
			if (dbUser is not null)
			{
				SocketGuildUser? guildUser = _client.GetGuild(Config.DdPalsId).GetUser(dbUser.DiscordId);
				if (guildUser is not null)
					userName = guildUser.Mention;
			}

			float oldScore = entryTuple.OldEntry.Time / 10000f;
			float newScore = entryTuple.NewEntry.Time / 10000f;
			_sb.Append(userName)
				.Append(" for getting a new PB of ")
				.Append(newScore)
				.Append("s! They beat their old PB of ")
				.AppendFormat("{0:0}", oldScore)
				.Append("s, gaining ")
				.Append(entryTuple.NewEntry.Rank - entryTuple.OldEntry.Rank)
				.Append(" ranks.");

			bool new1000Entry = oldScore < 1000 && newScore >= 1000;
			if (new1000Entry || oldScore < 1100 && newScore >= 1100)
			{
				int position = newEntries.Count(entry => entry.Time / 10000 >= (new1000Entry ? 1000 : 1100));
				string positionPostfix = (position % 10) switch
				{
					1 => "st",
					2 => "nd",
					3 => "rd",
					_ => "th",
				};

				_sb.Append(" They are the ")
					.Append(position)
					.Append(positionPostfix)
					.Append(new1000Entry ? " player to unlock the leviathan dagger!" : " 1100 player!");
			}

			if (entryTuple.NewEntry.Rank == 1)
				_sb.Append(Format.Bold(" It's a new WR! 👑 🎉"));

			await using Stream screenshot = await GetDdinfoPlayerScreenshot(entryTuple.NewEntry);
			await _ddNewsChannel!.SendFileAsync(screenshot, $"{entryTuple.NewEntry.Username}_{newScore:0}.png", _sb.ToString());
		}

		private async Task<Stream> GetDdinfoPlayerScreenshot(EntryResponse entry)
		{
			string ddinfoStyleCss = await File.ReadAllTextAsync("Data/DdinfoStyleCss.txt");
			string countryCode = await _webService.GetCountryCodeForplayer(entry.Id);
			string flagPath = Path.Combine("Data", "Flags", $"{countryCode}.png");
			if (countryCode.Length == 0 || !File.Exists(flagPath))
				countryCode = string.Empty;

			string html = $@"
			<html>
			<body style=""background-color:black;"">
				<div class=""goethe imagePadded"" style=""font-size: 50px; float: left;"">
					<div class=""rank"" style=""color:#dddddd; width: 25px; float: left;"">{entry.Rank}</div>
					{(countryCode.Length == 0 ? string.Empty : $"<div class=\"flag\" style=\"color:#dddddd; width: 55px; float: left;\"><img class=\"flag\" src=\"{flagPath}\"></div>")}
					<div class=""leviathan"" style=""width: 700px; float: left;"">axe</div>
					<div class=""leviathan"" style=""width: 185px; float: right;"">1163.3855</div>
				</div>
			</body>
			</html>
			{ddinfoStyleCss}";

			byte[] bytes = new HtmlConverter().FromHtmlString(html, 1040, ImageFormat.Png);
			return new MemoryStream(bytes);
		}
	}
}
