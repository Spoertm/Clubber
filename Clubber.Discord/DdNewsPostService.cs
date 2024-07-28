using Clubber.Discord.Helpers;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text;

namespace Clubber.Discord;

public class DdNewsPostService : RepeatingBackgroundService
{
	private const int _minimumScore = 930;

	private readonly AppConfig _config;
	private readonly IServiceScopeFactory _services;

	private readonly StringBuilder _sb = new();
	private readonly LeaderboardImageGenerator _imageGenerator = new();

	private SocketTextChannel? _ddNewsChannel;

	public DdNewsPostService(
		IOptions<AppConfig> config,
		IServiceScopeFactory services)
	{
		_config = config.Value;
		_services = services;
	}

	protected override TimeSpan TickInterval => TimeSpan.FromMinutes(1);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		Log.Debug("Executing {Class}", GetType().Name);

		await using AsyncServiceScope scope = _services.CreateAsyncScope();
		IDatabaseHelper databaseHelper = scope.ServiceProvider.GetRequiredService<IDatabaseHelper>();
		IDiscordHelper discordHelper = scope.ServiceProvider.GetRequiredService<IDiscordHelper>();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		IWebService webService = scope.ServiceProvider.GetRequiredService<IWebService>();

		await databaseHelper.CleanUpNewsItems();
		_ddNewsChannel ??= discordHelper.GetTextChannel(_config.DdNewsChannelId);
		EntryResponse[] oldEntries = await dbContext.LeaderboardCache.AsNoTracking().ToArrayAsync(stoppingToken);
		ICollection<EntryResponse> newEntries = await webService.GetSufficientLeaderboardEntries(_minimumScore);

		if (newEntries.Count == 0)
		{
			Log.Information("Fetched zero new leaderboard entries");
			return;
		}

		(EntryResponse oldEntry, EntryResponse newEntry)[] entryTuples = oldEntries.Join(
				inner: newEntries,
				outerKeySelector: oldEntry => oldEntry.Id,
				innerKeySelector: newEntry => newEntry.Id,
				resultSelector: (oldEntry, newEntry) => (oldEntry, newEntry))
			.ToArray();

		bool cacheIsToBeRefreshed = newEntries.Count > oldEntries.Length || entryTuples.Any(e => e.oldEntry.Time != e.newEntry.Time);

		IEnumerable<(EntryResponse oldEntry, EntryResponse newEntry)> changedEntriesOver1000 = entryTuples
			.Where(e => e.newEntry.Time > e.oldEntry.Time && e.newEntry.Time / 10_000 >= 1000);

		foreach ((EntryResponse oldEntry, EntryResponse newEntry) in changedEntriesOver1000)
		{
			Log.Information("Posting news for player entry {@Player}", newEntry);

			int nth = newEntries.Count(entry => entry.Time / 1_000_000 >= newEntry.Time / 1_000_000);

			Log.Debug("Getting DD News message");
			string message = await GetDdNewsMessage(databaseHelper, discordHelper, oldEntry, newEntry, nth);

			Log.Debug("Getting country code");
			string? countryCode = await GetCountryCode(newEntry);

			Log.Debug("Getting DD News screenshot");
			using (MemoryStream screenshot = _imageGenerator.CreateImage(newEntry.Rank, newEntry.Username, newEntry.Time, countryCode))
			{
				Log.Debug("Sending DD News to Discord");
				await _ddNewsChannel.SendFileAsync(screenshot, $"{newEntry.Username}_{newEntry.Time}.png", message);
			}

			Log.Debug("Adding news item to database");
			await databaseHelper.AddDdNewsItem(oldEntry, newEntry, nth);
			continue;

			async Task<string?> GetCountryCode(EntryResponse entry)
			{
				try
				{
					return await webService.GetCountryCodeForplayer(entry.Id);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to fetch country code for EntryResponse {@Player}", entry);
					return null;
				}
			}
		}

		if (cacheIsToBeRefreshed)
		{
			Log.Information("Updating leaderboard cache");
			await databaseHelper.UpdateLeaderboardCache(newEntries);
		}
		else
		{
			Log.Debug("No DD News posting required");
		}

		_sb.Clear();
	}

	private async Task<string> GetDdNewsMessage(
		IDatabaseHelper databaseHelper,
		IDiscordHelper discordHelper,
		EntryResponse oldEntry,
		EntryResponse newEntry,
		int nth)
	{
		string userName = newEntry.Username;
		if (await databaseHelper.FindRegisteredUser(newEntry.Id) is { } dbUser && discordHelper.GetGuildUser(_config.DdPalsId, dbUser.DiscordId) is { } guildUser)
		{
			userName = guildUser.Mention;
		}

		DdNewsMessageBuilder messageBuilder = new();
		string msg = messageBuilder.Build(
			userName,
			oldEntry.Time,
			oldEntry.Rank,
			newEntry.Time,
			newEntry.Rank, nth);

		return msg;
	}
}
