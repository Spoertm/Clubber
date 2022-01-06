using Clubber.Extensions;
using Clubber.Helpers;
using Clubber.Models.Responses;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Clubber.BackgroundTasks;

public class DdNewsPostService : AbstractBackgroundService
{
	private const int _minimumScore = 930;
	private SocketTextChannel? _ddNewsChannel;
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IDiscordHelper _discordHelper;
	private readonly IWebService _webService;
	private readonly LoggingService _loggingService;
	private readonly StringBuilder _sb = new();
	private readonly IServiceScopeFactory _services;
	private readonly ImageGenerator _imageGenerator = new();
	private static readonly int[] _exceptionPlayerIds = { 1 };

	public DdNewsPostService(
		IDatabaseHelper databaseHelper,
		IDiscordHelper discordHelper,
		IWebService webService,
		LoggingService loggingService,
		IServiceScopeFactory services)
		: base(loggingService)
	{
		_databaseHelper = databaseHelper;
		_discordHelper = discordHelper;
		_webService = webService;
		_loggingService = loggingService;
		_services = services;
	}

	protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		await _databaseHelper.CleanUpNewsItems();
		_ddNewsChannel ??= _discordHelper.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("DdNewsChannelId")!));
		using IServiceScope scope = _services.CreateScope();
		await using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
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
			if (oldEntry.Time == newEntry.Time)
				continue;

			cacheIsToBeRefreshed = true;
			if (!_exceptionPlayerIds.Contains(oldEntry.Id) && newEntry.Time / 10000 < 1000)
				continue;

			int nth = newEntries.Count(entry => entry.Time / 1000000 >= newEntry.Time / 1000000);
			await _databaseHelper.AddDdNewsItem(oldEntry, newEntry, nth);
			string message = GetDdNewsMessage(oldEntry, newEntry, nth);
			string countryCode = await _webService.GetCountryCodeForplayer(newEntry.Id);
			await using MemoryStream screenshot = await _imageGenerator.FromEntryResponse(newEntry, countryCode);
			await _ddNewsChannel.SendFileAsync(screenshot, $"{newEntry.Username}_{newEntry.Time}.png", message);
		}

		if (cacheIsToBeRefreshed)
		{
			await _loggingService.LogAsync(new(LogSeverity.Info, nameof(DdNewsPostService), "Updating leaderboard cache"));
			await _databaseHelper.UpdateLeaderboardCache(newEntries);
		}

		_sb.Clear();
	}

	private string GetDdNewsMessage(EntryResponse oldEntry, EntryResponse newEntry, int nth)
	{
		string userName = newEntry.Username;
		ulong ddPalsId = ulong.Parse(Environment.GetEnvironmentVariable("DdPalsId")!);
		if (_databaseHelper.GetDdUserBy(newEntry.Id) is { } dbUser && _discordHelper.GetGuildUser(ddPalsId, dbUser.DiscordId) is { } guildUser)
			userName = guildUser.Mention;

		double oldScore = oldEntry.Time / 10000d;
		double newScore = newEntry.Time / 10000d;
		int ranksChanged = oldEntry.Rank - newEntry.Rank;
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

		int oldHundredth = oldEntry.Time / 1000000;
		int newHundredth = newEntry.Time / 1000000;
		if (newHundredth > oldHundredth)
		{
			_sb.Append(" They are the ")
				.Append(nth)
				.Append(nth.OrdinalIndicator());

			if (oldHundredth == 9 && newHundredth == 10)
				_sb.Append(" player to unlock the leviathan dagger!");
			else
				_sb.Append($" {newHundredth * 100} player!");
		}

		if (newEntry.Rank == 1)
			_sb.Append(Format.Bold(" It's a new WR! 👑 🎉"));

		return _sb.ToString();
	}
}
