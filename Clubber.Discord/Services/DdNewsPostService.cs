using Clubber.Discord.Helpers;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace Clubber.Discord.Services;

public sealed class DdNewsPostService(
    IOptions<AppConfig> config,
    IServiceScopeFactory services) : RepeatingBackgroundService
{
    private const int NewsWorthyThreshold = 1000;
    private const int GetRecentLimit = 100;

    private readonly AppConfig _config = config.Value;
    private readonly LeaderboardImageGenerator _imageGenerator = new();

    protected override TimeSpan TickInterval => TimeSpan.FromMinutes(1);

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        ServiceCollection serviceCollection = new(scope);

        await serviceCollection.NewsRepository.RemoveOlderThanAsync(TimeSpan.FromDays(1));

        DateTimeOffset? lastCheck = await serviceCollection.AppStateRepository.GetLastNewsCheckAsync();
        if (lastCheck is null)
        {
            lastCheck = DateTimeOffset.UtcNow.AddMinutes(-5);
            await serviceCollection.AppStateRepository.SetLastNewsCheckAsync(lastCheck.Value);
        }

        List<GetRecentResponse> recentResponses = await FetchRecentRunsWithBackfillAsync(lastCheck.Value, serviceCollection);
        if (recentResponses.Count == 0)
        {
            await serviceCollection.AppStateRepository.SetLastNewsCheckAsync(DateTimeOffset.UtcNow);
            return;
        }

        GetRecentResponse[] recentRuns = recentResponses
            .Where(p => p.Timestamp > lastCheck)
            .OrderBy(p => p.Timestamp)
            .ToArray();

        if (recentRuns.Length == 0)
        {
            await serviceCollection.AppStateRepository.SetLastNewsCheckAsync(DateTimeOffset.UtcNow);
            return;
        }

        List<NewsUpdate> newsUpdates = [];
        List<(PlayerPb Pb, bool IsNewsWorthy, int Nth)> pendingUpserts = [];
        foreach (GetRecentResponse submission in recentRuns)
        {
            PlayerPb? pb = await serviceCollection.PlayerPbRepository.GetByIdAsync(submission.LeaderboardId);

            if (pb != null && submission.Time <= pb.Time)
            {
                continue;
            }

            bool isNewsWorthy = pb != null && submission.Time >= NewsWorthyThreshold * 10_000;

            int oldTime = pb?.Time ?? 0;
            int newTime = submission.Time;
            int oldHundredth = oldTime / 1_000_000;
            int newHundredth = newTime / 1_000_000;
            int nth = 0;

            if (newHundredth > oldHundredth)
            {
                for (int h = oldHundredth + 1; h <= newHundredth; h++)
                {
                    await serviceCollection.HundredthCountRepository.IncrementAsync(h * 100);
                }
            }

            if (newHundredth >= 10)
            {
                nth = await serviceCollection.HundredthCountRepository.GetCountAsync(newHundredth * 100);
            }

            pendingUpserts.Add((new PlayerPb
            {
                LeaderboardId = submission.LeaderboardId,
                Username = submission.UserName,
                Time = submission.Time,
                Rank = pb?.Rank ?? 0,
                LastUpdated = submission.Timestamp,
            }, isNewsWorthy, nth));

            if (isNewsWorthy)
            {
                newsUpdates.Add(new NewsUpdate(
                    new EntryResponse { Id = pb!.LeaderboardId, Username = pb.Username, Time = pb.Time, Rank = pb.Rank },
                    new EntryResponse { Id = submission.LeaderboardId, Username = submission.UserName, Time = submission.Time, Rank = 0 },
                    nth));
            }
        }

        if (pendingUpserts.Count > 0)
        {
            uint[] ids = pendingUpserts.Select(p => p.Pb.LeaderboardId).Distinct().ToArray();
            IReadOnlyList<EntryResponse> currentPlayers = await serviceCollection.WebService.GetLbPlayers(ids);
            Dictionary<uint, EntryResponse> rankLookup = currentPlayers.ToDictionary(p => p.Id);

            foreach ((PlayerPb pb, _, _) in pendingUpserts)
            {
                if (rankLookup.TryGetValue(pb.LeaderboardId, out EntryResponse? entry))
                {
                    pb.Rank = entry.Rank;
                }

                await serviceCollection.PlayerPbRepository.UpsertAsync(pb);
            }

            foreach (NewsUpdate update in newsUpdates)
            {
                if (rankLookup.TryGetValue(update.NewEntry.Id, out EntryResponse? entry))
                {
                    update.NewEntry.Rank = entry.Rank;
                }
            }
        }

        await PublishNewsIfAvailable(newsUpdates, serviceCollection);
        await serviceCollection.AppStateRepository.SetLastNewsCheckAsync(recentRuns[^1].Timestamp);
    }

    private async Task PublishNewsIfAvailable(IEnumerable<NewsUpdate> newsUpdates, ServiceCollection serviceCollection)
    {
        SocketTextChannel channel = serviceCollection.DiscordHelper.GetTextChannel(_config.DdNewsChannelId);
        foreach (NewsUpdate update in newsUpdates)
        {
            Log.Information("Publishing news for {Player} - {Score}s",
                update.NewEntry.Username, update.NewEntry.Time / 10_000d);

            try
            {
                await PublishSingleNews(update, channel, serviceCollection);
                DdNewsItem newsItem = new()
                {
                    LeaderboardId = update.NewEntry.Id,
                    OldEntry = update.OldEntry,
                    NewEntry = update.NewEntry,
                    TimeOfOccurenceUtc = DateTimeOffset.UtcNow,
                    Nth = update.Nth,
                };

                await serviceCollection.NewsRepository.AddAsync(newsItem);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to publish news for player {PlayerId}", update.NewEntry.Id);
            }
        }
    }

    private async Task PublishSingleNews(NewsUpdate update, SocketTextChannel channel, ServiceCollection serviceCollection)
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

        DdUser? dbUser = await serviceCollection.UserRepository.FindAsync(update.NewEntry.Id);
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

    private static async Task<string?> GetCountryCode(uint playerLbId, ServiceCollection serviceCollection)
    {
        try
        {
            return await serviceCollection.WebService.GetCountryCodeForplayer(playerLbId);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not fetch country code for player {PlayerId}", playerLbId);
            return null;
        }
    }

    private static async Task<List<GetRecentResponse>> FetchRecentRunsWithBackfillAsync(DateTimeOffset lastCheck, ServiceCollection serviceCollection)
    {
        List<GetRecentResponse> allRuns = [];
        DateTime fetchBefore = DateTime.UtcNow;

        while (true)
        {
            IReadOnlyList<GetRecentResponse> batch = await serviceCollection.WebService.GetRecentScores(fetchBefore, GetRecentLimit);

            if (batch.Count == 0)
            {
                break;
            }

            allRuns.AddRange(batch);

            GetRecentResponse oldestInBatch = batch.MinBy(r => r.Timestamp)!;

            if (batch.Count < GetRecentLimit || oldestInBatch.Timestamp <= lastCheck)
            {
                break;
            }

            fetchBefore = oldestInBatch.Timestamp.UtcDateTime;
        }

        return allRuns;
    }

    // Helper types for better organization
    private sealed record ServiceCollection(
        INewsRepository NewsRepository,
        IUserRepository UserRepository,
        IPlayerPbRepository PlayerPbRepository,
        IHundredthCountRepository HundredthCountRepository,
        IAppStateRepository AppStateRepository,
        IDiscordHelper DiscordHelper,
        IWebService WebService)
    {
        public ServiceCollection(IServiceScope scope) : this(
            scope.ServiceProvider.GetRequiredService<INewsRepository>(),
            scope.ServiceProvider.GetRequiredService<IUserRepository>(),
            scope.ServiceProvider.GetRequiredService<IPlayerPbRepository>(),
            scope.ServiceProvider.GetRequiredService<IHundredthCountRepository>(),
            scope.ServiceProvider.GetRequiredService<IAppStateRepository>(),
            scope.ServiceProvider.GetRequiredService<IDiscordHelper>(),
            scope.ServiceProvider.GetRequiredService<IWebService>())
        {
        }
    }

    private readonly record struct NewsUpdate(
        EntryResponse OldEntry,
        EntryResponse NewEntry,
        int Nth);
}
