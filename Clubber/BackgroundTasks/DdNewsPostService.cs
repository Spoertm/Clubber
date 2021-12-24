using Clubber.Extensions;
using Clubber.Helpers;
using Clubber.Models.Responses;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Text;

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
		private readonly IServiceProvider _services;

		public DdNewsPostService(
			IDatabaseHelper databaseHelper,
			IDiscordHelper discordHelper,
			IWebService webService,
			LoggingService loggingService,
			ImageGenerator imageGenerator,
			IServiceProvider services)
			: base(loggingService)
		{
			_databaseHelper = databaseHelper;
			_discordHelper = discordHelper;
			_webService = webService;
			_imageGenerator = imageGenerator;
			_services = services;
		}

		protected override TimeSpan Interval => TimeSpan.FromMinutes(2);

		protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
		{
			_ddNewsChannel ??= _discordHelper.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("DdNewsChannelId")!));
			await using DatabaseService dbContext = _services.GetRequiredService<DatabaseService>();
			List<EntryResponse> oldEntries = dbContext.LeaderboardCache.AsNoTracking().ToList();
			List<EntryResponse> newEntries = await _webService.GetSufficientLeaderboardEntries(_minimumScore);
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

		private string GetDdNewsMessage(List<EntryResponse> newEntries, (EntryResponse OldEntry, EntryResponse NewEntry) entryTuple)
		{
			string userName = entryTuple.NewEntry.Username;
			ulong ddPalsId = ulong.Parse(Environment.GetEnvironmentVariable("DdPalsId")!);
			if (_databaseHelper.GetDdUserBy(ddu => ddu.LeaderboardId, entryTuple.NewEntry.Id) is { } dbUser && _discordHelper.GetGuildUser(ddPalsId, dbUser.DiscordId) is { } guildUser)
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
				.Append(ranksChanged > 0 ? "gaining " : ranksChanged == 0 ? "but didn't change" : "but lost ")
				.Append(ranksChanged == 0 ? "" : Math.Abs(ranksChanged))
				.Append(Math.Abs(ranksChanged) is 1 or 0 ? " rank." : " ranks.");

			int oldHundredth = entryTuple.OldEntry.Time / 1000000;
			int newHundredth = entryTuple.NewEntry.Time / 1000000;
			if (newHundredth > oldHundredth)
			{
				int position = newEntries.Count(entry => entry.Time / 1000000 >= newHundredth);
				_sb.Append(" They are the ")
					.Append(position)
					.Append(position.OrdinalIndicator());

				if (oldHundredth == 9 && newHundredth == 10)
					_sb.Append(" player to unlock the leviathan dagger!");
				else
					_sb.Append($" {newHundredth * 100} player!");
			}

			if (entryTuple.NewEntry.Rank == 1)
				_sb.Append(Format.Bold(" It's a new WR! 👑 🎉"));

			return _sb.ToString();
		}
	}
}
