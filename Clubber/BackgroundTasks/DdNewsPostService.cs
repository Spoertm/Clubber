using Clubber.Extensions;
using Clubber.Helpers;
using Clubber.Models.Responses;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clubber.BackgroundTasks
{
	public class DdNewsPostService : AbstractBackgroundService
	{
		private const int _minimumScore = 930;
		private SocketTextChannel? _ddNewsChannel;
		private readonly IDatabaseHelper _databaseHelper;
		private readonly IDiscordHelper _discordHelper;
		private readonly IWebService _webService;
		private readonly StringBuilder _sb = new();
		private readonly ImageGenerator _imageGenerator;

		public DdNewsPostService(
			IDatabaseHelper databaseHelper,
			IDiscordHelper discordHelper,
			IWebService webService,
			LoggingService loggingService,
			ImageGenerator imageGenerator)
			: base(loggingService)
		{
			_databaseHelper = databaseHelper;
			_discordHelper = discordHelper;
			_webService = webService;
			_imageGenerator = imageGenerator;
		}

		protected override TimeSpan Interval => TimeSpan.FromMinutes(2);

		protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
		{
			_ddNewsChannel ??= _discordHelper.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("DdNewsChannelId")!));
			List<EntryResponse> oldEntries = _databaseHelper.LeaderboardCache;
			List<EntryResponse> newEntries = await GetSufficientLeaderboardEntries(_minimumScore);
			if (newEntries.Count == 0)
				return;

			(EntryResponse, EntryResponse)[] entryTuples = oldEntries.Join(
					inner: newEntries,
					outerKeySelector: oldEntry => oldEntry.Id,
					innerKeySelector: newEntry => newEntry.Id,
					resultSelector: (oldEntry, newEntry) => (oldEntry, newEntry))
				.ToArray();

			bool cacheIsToBeRefreshed = newEntries.Count > oldEntries.Count;
			foreach ((EntryResponse oldEntry, EntryResponse newEntry) in entryTuples)
			{
				if (oldEntry.Time != newEntry.Time)
					cacheIsToBeRefreshed = true;

				if (oldEntry.Time == newEntry.Time || newEntry.Time / 10000 < 1000)
					continue;

				cacheIsToBeRefreshed = true;
				string message = GetDdNewsMessage(newEntries, (oldEntry, newEntry));
				await using MemoryStream screenshot = await _imageGenerator.FromEntryResponse(newEntry);
				await _ddNewsChannel.SendFileAsync(screenshot, $"{newEntry.Username}_{newEntry.Time}.png", message);
			}

			if (cacheIsToBeRefreshed)
				await _databaseHelper.UpdateLeaderboardCache(newEntries);

			_sb.Clear();
		}

		private async Task<List<EntryResponse>> GetSufficientLeaderboardEntries(int minimumScore)
		{
			List<EntryResponse> entries = new();
			int rank = 1;
			do
			{
				try
				{
					entries.AddRange((await _webService.GetLeaderboardEntries(rank)).Entries);
				}
				catch
				{
					return new();
				}

				rank += 100;
				await Task.Delay(50);
			}
			while (entries[^1].Time / 10000 >= minimumScore);

			return entries;
		}

		private string GetDdNewsMessage(List<EntryResponse> newEntries, (EntryResponse OldEntry, EntryResponse NewEntry) entryTuple)
		{
			string userName = entryTuple.NewEntry.Username;
			ulong ddPalsId = ulong.Parse(Environment.GetEnvironmentVariable("DdPalsId")!);
			if (_databaseHelper.GetDdUserByLbId(entryTuple.NewEntry.Id) is { } dbUser && _discordHelper.GetGuildUser(ddPalsId, dbUser.DiscordId) is { } guildUser)
				userName = guildUser.Mention;

			double oldScore = entryTuple.OldEntry.Time / 10000d;
			double newScore = entryTuple.NewEntry.Time / 10000d;
			int ranksChanged = entryTuple.OldEntry.Rank - entryTuple.NewEntry.Rank;
			_sb.Clear()
				.Append("Congratulations to ")
				.Append(userName)
				.Append(" for getting a new PB of ")
				.AppendFormat("{0:0.0000}", newScore)
				.Append(" seconds! They beat their old PB of ")
				.AppendFormat("{0:0.0000}", oldScore)
				.Append("s (**+")
				.AppendFormat("{0:0.0000}", newScore - oldScore)
				.Append("s**), ")
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
	}
}
