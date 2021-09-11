using Clubber.Configuration;
using Clubber.Extensions;
using Clubber.Helpers;
using Clubber.Models.Responses;
using Clubber.Services;
using CoreHtmlToImage;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ImageFormat = CoreHtmlToImage.ImageFormat;

namespace Clubber.BackgroundTasks
{
	public class DdNewsPostService : AbstractBackgroundService
	{
		private const int _minimumScore = 930;
		private readonly IConfig _config;
		private SocketTextChannel? _ddNewsChannel;
		private readonly IDatabaseHelper _databaseHelper;
		private readonly IDiscordHelper _discordHelper;
		private readonly IIOService _ioService;
		private readonly IWebService _webService;
		private readonly StringBuilder _sb = new();
		private readonly HtmlConverter _htmlConverter = new();

		public DdNewsPostService(
			IConfig config,
			IDatabaseHelper databaseHelper,
			IDiscordHelper discordHelper,
			IIOService ioService,
			IWebService webService,
			LoggingService loggingService)
			: base(loggingService)
		{
			_config = config;
			_databaseHelper = databaseHelper;
			_discordHelper = discordHelper;
			_ioService = ioService;
			_webService = webService;

			Directory.CreateDirectory(Path.GetDirectoryName(LbCachePath)!);
			string latestAttachmentUrl = _discordHelper.GetLatestAttachmentUrlFromChannel(_config.LbEntriesCacheChannelId).Result;
			string databaseJson = _webService.RequestStringAsync(latestAttachmentUrl).Result;
			File.WriteAllText(LbCachePath, databaseJson);
		}

		protected override TimeSpan Interval => TimeSpan.FromMinutes(2);
		private static string LbCachePath => Path.Combine(AppContext.BaseDirectory, "LeaderboardCache.json");

		protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
		{
			_ddNewsChannel ??= _discordHelper.GetTextChannel(_config.DdNewsChannelId);
			List<EntryResponse> oldEntries = (await _ioService.ReadObjectFromFile<List<EntryResponse>>(LbCachePath))!;
			List<EntryResponse> newEntries = await GetSufficientLeaderboardEntries(_minimumScore);
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
				string message = GetDdNewsMessage(newEntries, (oldEntry, newEntry));
				Stream screenshot = await GetDdinfoPlayerScreenshot(newEntry);
				await _ddNewsChannel.SendFileAsync(screenshot, $"{newEntry.Username}_{newEntry.Time}.png", message);
			}

			if (cacheIsToBeRefreshed)
			{
				await _ioService.WriteObjectToFile(newEntries, LbCachePath);
				await _discordHelper.SendFileToChannel(LbCachePath, _config.LbEntriesCacheChannelId);
			}
		}

		private async Task<List<EntryResponse>> GetSufficientLeaderboardEntries(int minimumScore)
		{
			List<EntryResponse> entries = new();
			int rank = 1;
			do
			{
				entries.AddRange((await _webService.GetLeaderboardEntries(rank)).Entries);
				rank += 100;
				await Task.Delay(50);
			}
			while (entries[^1].Time / 10000 >= minimumScore);

			return entries;
		}

		private string GetDdNewsMessage(List<EntryResponse> newEntries, (EntryResponse OldEntry, EntryResponse NewEntry) entryTuple)
		{
			_sb.Clear().Append("Congratulations to ");
			string userName = entryTuple.NewEntry.Username;
			if (_databaseHelper.GetDdUserByLbId(entryTuple.NewEntry.Id) is { } dbUser && _discordHelper.GetGuildUser(_config.DdPalsId, dbUser.DiscordId) is { } guildUser)
				userName = guildUser.Mention;

			double oldScore = entryTuple.OldEntry.Time / 10000d;
			double newScore = entryTuple.NewEntry.Time / 10000d;
			int ranksChanged = entryTuple.OldEntry.Rank - entryTuple.NewEntry.Rank;
			_sb.Append(userName)
				.Append(" for getting a new PB of ")
				.AppendFormat("{0:0.0000}", newScore)
				.Append(" seconds! They beat their old PB of ")
				.Append(Math.Truncate(oldScore))
				.Append("s, ")
				.Append(ranksChanged >= 0 ? "gaining " : "but lost ")
				.Append(Math.Abs(ranksChanged))
				.Append(Math.Abs(ranksChanged) == 1 ? " rank." : " ranks.");

			bool new1000Entry = oldScore < 1000 && newScore >= 1000;
			if (new1000Entry || oldScore < 1100 && newScore >= 1100)
			{
				int position = newEntries.Count(entry => entry.Time / 10000 >= (new1000Entry ? 1000 : 1100));
				_sb.Append(" They are the ")
					.Append(position)
					.Append(position.OrdinalIndicator())
					.Append(new1000Entry ? " player to unlock the leviathan dagger!" : " 1100 player!");
			}

			if (entryTuple.NewEntry.Rank == 1)
				_sb.Append(Format.Bold(" It's a new WR! 👑 🎉"));

			return _sb.ToString();
		}

		private async Task<Stream> GetDdinfoPlayerScreenshot(EntryResponse entry)
		{
			string countryCode = await _webService.GetCountryCodeForplayer(entry.Id);
			string flagPath = Path.Combine(AppContext.BaseDirectory, "Data", "Flags", $"{countryCode}.png");
			if (countryCode.Length == 0 || !File.Exists(flagPath))
				flagPath = Path.Combine(AppContext.BaseDirectory, "Data", "Flags", "00.png");

			string ddinfoStyleHtml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "DdinfoStyle.html"));
			string flagBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(flagPath));
			string formattedHtml = string.Format(
				ddinfoStyleHtml,
				entry.Rank,
				flagBase64,
				HttpUtility.HtmlEncode(entry.Username),
				$"{entry.Time / 10000d:0.0000}");

			byte[] bytes = _htmlConverter.FromHtmlString(formattedHtml, 1100, ImageFormat.Png);
			return new MemoryStream(bytes);
		}
	}
}
