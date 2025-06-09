using Clubber.Discord.Helpers;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace Clubber.Discord.Services;

public sealed class DdNewsPostService(
	IOptions<AppConfig> config,
	IServiceScopeFactory services) : RepeatingBackgroundService
{
	private const int _minimumScoreToTrack = 930;
	private const int _newsWorthyThreshold = 1000;

	private readonly AppConfig _config = config.Value;
	private readonly LeaderboardImageGenerator _imageGenerator = new();

	protected override TimeSpan TickInterval => TimeSpan.FromMinutes(1);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		await using AsyncServiceScope scope = services.CreateAsyncScope();
		ServiceCollection serviceCollection = new(scope);

		await serviceCollection.DatabaseHelper.CleanUpNewsItems();

		LeaderboardSnapshot leaderboardData = await GetLeaderboardData(serviceCollection, stoppingToken);
		if (leaderboardData.IsEmpty)
		{
			Log.Information("No leaderboard data available");
			return;
		}

		IEnumerable<NewsUpdate> newsUpdates = DetectNewsWorthyUpdates(leaderboardData);
		await PublishNewsIfAvailable(newsUpdates, serviceCollection);

		if (leaderboardData.HasChanges)
		{
			await UpdateCache(leaderboardData.NewEntries, serviceCollection);
		}
	}

	private static async Task<LeaderboardSnapshot> GetLeaderboardData(
		ServiceCollection services, CancellationToken cancellationToken)
	{
		Dictionary<int, EntryResponse> currentEntries = await services.DbContext.LeaderboardCache
			.AsNoTracking()
			.ToDictionaryAsync(e => e.Id, cancellationToken);

		ICollection<EntryResponse> newEntries = await services.WebService.GetSufficientLeaderboardEntries(_minimumScoreToTrack);
		if (newEntries.Count == 0)
		{
			return LeaderboardSnapshot.Empty;
		}

		bool hasChanges = LeaderboardChanged(currentEntries.Values, newEntries);
		return new LeaderboardSnapshot(currentEntries, newEntries, hasChanges);
	}

	private static bool LeaderboardChanged(
		IEnumerable<EntryResponse> oldEntries, ICollection<EntryResponse> newEntries)
	{
		Dictionary<int, int> oldLookup = oldEntries.ToDictionary(e => e.Id, e => e.Time);
		return newEntries.Count != oldLookup.Count ||
		       newEntries.Any(entry => !oldLookup.TryGetValue(entry.Id, out int oldTime) ||
		                               oldTime != entry.Time);
	}

	private static IEnumerable<NewsUpdate> DetectNewsWorthyUpdates(LeaderboardSnapshot snapshot)
	{
		return snapshot.NewEntries
			.Where(newEntry => snapshot.CurrentEntries.TryGetValue(newEntry.Id, out EntryResponse? oldEntry) &&
			                   newEntry.Time > oldEntry.Time &&
			                   newEntry.Time >= _newsWorthyThreshold * 10_000)
			.Select(newEntry => CreateNewsUpdate(newEntry, snapshot.CurrentEntries[newEntry.Id], snapshot.NewEntries));

		static NewsUpdate CreateNewsUpdate(
			EntryResponse newEntry, EntryResponse oldEntry, ICollection<EntryResponse> allNewEntries)
		{
			int nth = allNewEntries.Count(e => e.Time / 1_000_000 >= newEntry.Time / 1_000_000);
			return new NewsUpdate(oldEntry, newEntry, nth);
		}
	}

	private async Task PublishNewsIfAvailable(IEnumerable<NewsUpdate> newsUpdates, ServiceCollection serviceCollection)
	{
		SocketTextChannel? channel = null;

		foreach (NewsUpdate update in newsUpdates)
		{
			Log.Information("Publishing news for {Player} - {Score}s",
				update.NewEntry.Username, update.NewEntry.Time / 10_000d);

			try
			{
				channel ??= serviceCollection.DiscordHelper.GetTextChannel(_config.DdNewsChannelId);
				await PublishSingleNews(update, channel, serviceCollection);
				await serviceCollection.DatabaseHelper.AddDdNewsItem(update.OldEntry, update.NewEntry, update.Nth);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to publish news for player {PlayerId}", update.NewEntry.Id);
			}
		}
	}

	private async Task PublishSingleNews(
		NewsUpdate update, SocketTextChannel channel, ServiceCollection serviceCollection)
	{
		string message = await CreateNewsMessage(update, serviceCollection);
		string? countryCode = await GetCountryCode(update.NewEntry.Id, serviceCollection);

		using MemoryStream image = _imageGenerator.CreateImage(
			update.NewEntry.Rank,
			update.NewEntry.Username,
			update.NewEntry.Time,
			countryCode);

		string filename = $"{update.NewEntry.Username}_{update.NewEntry.Time}.png";
		await channel.SendFileAsync(image, filename, message);
	}

	private async Task<string> CreateNewsMessage(NewsUpdate update, ServiceCollection serviceCollection)
	{
		string? username = update.NewEntry.Username;

		// Try to get Discord mention if user is registered
		DdUser? dbUser = await serviceCollection.DatabaseHelper.FindRegisteredUser(update.NewEntry.Id);
		if (dbUser != null)
		{
			SocketGuildUser? guildUser = serviceCollection.DiscordHelper.GetGuildUser(_config.DdPalsId, dbUser.DiscordId);
			if (guildUser != null)
			{
				username = guildUser.Mention;
			}
		}

		DdNewsMessageBuilder messageBuilder = new();
		return messageBuilder.Build(
			username,
			update.OldEntry.Time,
			update.OldEntry.Rank,
			update.NewEntry.Time,
			update.NewEntry.Rank,
			update.Nth);
	}

	private static async Task<string?> GetCountryCode(int playerId, ServiceCollection serviceCollection)
	{
		try
		{
			return await serviceCollection.WebService.GetCountryCodeForplayer(playerId);
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Could not fetch country code for player {PlayerId}", playerId);
			return null;
		}
	}

	private static async Task UpdateCache(ICollection<EntryResponse> newEntries, ServiceCollection serviceCollection)
	{
		Log.Information("Updating leaderboard cache with {Count} entries", newEntries.Count);
		await serviceCollection.DatabaseHelper.UpdateLeaderboardCache(newEntries);
	}

	// Helper types for better organization
	private sealed record ServiceCollection(
		IDatabaseHelper DatabaseHelper,
		IDiscordHelper DiscordHelper,
		DbService DbContext,
		IWebService WebService)
	{
		public ServiceCollection(IServiceScope scope) : this(
			scope.ServiceProvider.GetRequiredService<IDatabaseHelper>(),
			scope.ServiceProvider.GetRequiredService<IDiscordHelper>(),
			scope.ServiceProvider.GetRequiredService<DbService>(),
			scope.ServiceProvider.GetRequiredService<IWebService>())
		{
		}
	}

	private readonly record struct LeaderboardSnapshot(
		IReadOnlyDictionary<int, EntryResponse> CurrentEntries,
		ICollection<EntryResponse> NewEntries,
		bool HasChanges)
	{
		public bool IsEmpty => NewEntries.Count == 0;
		public static LeaderboardSnapshot Empty => new(new Dictionary<int, EntryResponse>(), [], false);
	}

	private readonly record struct NewsUpdate(
		EntryResponse OldEntry,
		EntryResponse NewEntry,
		int Nth);
}
