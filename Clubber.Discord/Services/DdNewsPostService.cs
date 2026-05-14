using Clubber.Discord.Helpers;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Clubber.Domain.Data;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord.Net;
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
    private const int NewsWorthyThreshold = 1000;
    private const int MinimumTrackableScore = 700;
    private const int GetRecentLimit = 100;

    private readonly AppConfig _config = config.Value;
    private readonly LeaderboardImageGenerator _imageGenerator = new();

    protected override TimeSpan TickInterval => TimeSpan.FromMinutes(1);

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        ServiceCollection sc = new(scope);

        await sc.NewsRepository.RemoveOlderThanAsync(TimeSpan.FromDays(1));

        DateTimeOffset lastCheck = await GetLastCheckAsync(sc.DbContext);
        List<GetRecentResponse> responses = await FetchRecentRunsWithBackfillAsync(lastCheck, sc.WebService);

        GetRecentResponse[] recentRuns = responses
            .Where(r => r.Timestamp >= lastCheck)
            .Where(r => !string.IsNullOrWhiteSpace(r.UserName))
            .OrderBy(r => r.Timestamp)
            .ToArray();

        if (recentRuns.Length == 0)
        {
            if (responses.Count == 0)
            {
                await UpdateLastCheckAsync(sc.DbContext, DateTimeOffset.UtcNow);
            }

            return;
        }

        uint[] runIds = recentRuns.Select(r => r.LeaderboardId).Distinct().ToArray();
        Dictionary<uint, PlayerPb> existingPbs = await sc.DbContext.PlayerPbs
            .AsNoTracking()
            .Where(p => runIds.Contains(p.LeaderboardId))
            .ToDictionaryAsync(p => p.LeaderboardId, cancellationToken: stoppingToken);

        HashSet<int> hundredthsToTrack = GetRelevantHundredths(recentRuns, existingPbs);
        Dictionary<int, int> currentHundredthCounts = await sc.DbContext.HundredthCounts
            .AsNoTracking()
            .Where(h => hundredthsToTrack.Contains(h.Threshold))
            .ToDictionaryAsync(h => h.Threshold, h => h.Count, cancellationToken: stoppingToken);

        ProcessRunsResult result = ProcessRuns(recentRuns, existingPbs, currentHundredthCounts);

        if (result.Upserts.Count > 0)
        {
            await ApplyRanksAsync(result, sc);
        }

        await PublishNewsIfAvailable(result.NewsUpdates, sc);
        await UpdateLastCheckAsync(sc.DbContext, recentRuns[^1].Timestamp);
        await sc.DbContext.SaveChangesAsync(stoppingToken);
    }

    internal static ProcessRunsResult ProcessRuns(
        GetRecentResponse[] recentRuns,
        IReadOnlyDictionary<uint, PlayerPb> existingPbs,
        IReadOnlyDictionary<int, int> currentHundredthCounts)
    {
        List<PlayerPb> upserts = [];
        List<NewsUpdate> newsUpdates = [];
        Dictionary<int, int> hundredthChanges = new(currentHundredthCounts);
        Dictionary<uint, PlayerPb> pbLookup = new(existingPbs);
        Dictionary<uint, PlayerPb> newsBaseline = new(existingPbs);

        foreach (GetRecentResponse run in recentRuns)
        {
            if (string.IsNullOrWhiteSpace(run.UserName))
            {
                continue;
            }

            PlayerPb? oldPb = pbLookup.GetValueOrDefault(run.LeaderboardId);
            if (oldPb != null && run.Time <= oldPb.Time)
            {
                continue;
            }

            if (oldPb == null && run.Time < MinimumTrackableScore * 10_000)
            {
                continue;
            }

            int oldHundredth = oldPb?.Time / 1_000_000 ?? 0;
            int newHundredth = run.Time / 1_000_000;

            PlayerPb newPb = new()
            {
                LeaderboardId = run.LeaderboardId,
                Username = run.UserName,
                Time = run.Time,
                Rank = 0,
                LastUpdated = run.Timestamp,
            };

            upserts.Add(newPb);
            pbLookup[run.LeaderboardId] = newPb;

            for (int h = Math.Max(oldHundredth + 1, MinimumTrackableScore / 100); h <= newHundredth; h++)
            {
                int threshold = h * 100;
                hundredthChanges[threshold] = hundredthChanges.GetValueOrDefault(threshold) + 1;
            }

            PlayerPb? newsOldPb = newsBaseline.GetValueOrDefault(run.LeaderboardId);
            if (newsOldPb != null && run.Time >= NewsWorthyThreshold * 10_000 && run.Time > newsOldPb.Time)
            {
                int nth = 0;
                if (newHundredth > oldHundredth)
                {
                    int threshold = newHundredth * 100;
                    nth = hundredthChanges[threshold];
                }

                newsUpdates.Add(new NewsUpdate(
                    new EntryResponse { Id = newsOldPb.LeaderboardId, Username = newsOldPb.Username, Time = newsOldPb.Time, Rank = newsOldPb.Rank },
                    new EntryResponse { Id = run.LeaderboardId, Username = run.UserName, Time = run.Time, Rank = 0 },
                    nth));

                newsBaseline[run.LeaderboardId] = newPb;
            }
        }

        List<PlayerPb> dedupedUpserts = upserts
            .GroupBy(p => p.LeaderboardId)
            .Select(g => g.Last())
            .ToList();

        return new ProcessRunsResult(dedupedUpserts, newsUpdates, hundredthChanges);
    }

    private static HashSet<int> GetRelevantHundredths(GetRecentResponse[] recentRuns, IReadOnlyDictionary<uint, PlayerPb> existingPbs)
    {
        HashSet<int> hundredths = [];
        foreach (GetRecentResponse run in recentRuns)
        {
            PlayerPb? oldPb = existingPbs.GetValueOrDefault(run.LeaderboardId);
            if (oldPb != null && run.Time <= oldPb.Time)
                continue;

            int oldH = oldPb?.Time / 1_000_000 ?? 0;
            int newH = run.Time / 1_000_000;
            for (int h = Math.Max(oldH + 1, MinimumTrackableScore / 100); h <= newH; h++)
                hundredths.Add(h * 100);
        }

        return hundredths;
    }

    private static async Task ApplyRanksAsync(ProcessRunsResult result, ServiceCollection sc)
    {
        uint[] ids = result.Upserts.Select(p => p.LeaderboardId).Distinct().ToArray();
        IReadOnlyList<EntryResponse> currentPlayers = await sc.WebService.GetLbPlayers(ids);
        Dictionary<uint, EntryResponse> rankLookup = currentPlayers.ToDictionary(p => p.Id);

        uint[] existingIds = result.Upserts.Select(p => p.LeaderboardId).ToArray();
        Dictionary<uint, PlayerPb> existingPbs = await sc.DbContext.PlayerPbs
            .Where(p => existingIds.Contains(p.LeaderboardId))
            .ToDictionaryAsync(p => p.LeaderboardId);

        foreach (PlayerPb pb in result.Upserts)
        {
            if (rankLookup.TryGetValue(pb.LeaderboardId, out EntryResponse? entry))
            {
                pb.Rank = entry.Rank;
            }

            if (existingPbs.TryGetValue(pb.LeaderboardId, out PlayerPb? existing))
            {
                existing.Username = pb.Username;
                existing.Time = pb.Time;
                existing.Rank = pb.Rank;
                existing.LastUpdated = pb.LastUpdated;
            }
            else
            {
                sc.DbContext.PlayerPbs.Add(pb);
            }
        }

        foreach (NewsUpdate update in result.NewsUpdates)
        {
            if (rankLookup.TryGetValue(update.NewEntry.Id, out EntryResponse? entry))
            {
                update.NewEntry.Rank = entry.Rank;
            }
        }

        int[] allThresholds = result.HundredthChanges.Keys.ToArray();
        Dictionary<int, int> dbCounts = await sc.DbContext.HundredthCounts
            .Where(h => allThresholds.Contains(h.Threshold))
            .ToDictionaryAsync(h => h.Threshold, h => h.Count);

        int[] modifiedThresholds = result.HundredthChanges
            .Where(kvp => !dbCounts.TryGetValue(kvp.Key, out int existingCount) || existingCount != kvp.Value)
            .Select(kvp => kvp.Key)
            .ToArray();

        if (modifiedThresholds.Length > 0)
        {
            Dictionary<int, HundredthCount> existingCounts = await sc.DbContext.HundredthCounts
                .Where(h => modifiedThresholds.Contains(h.Threshold))
                .ToDictionaryAsync(h => h.Threshold);

            foreach (int threshold in modifiedThresholds)
            {
                int count = result.HundredthChanges[threshold];
                if (existingCounts.TryGetValue(threshold, out HundredthCount? existing))
                {
                    existing.Count = count;
                }
                else
                {
                    sc.DbContext.HundredthCounts.Add(new HundredthCount { Threshold = threshold, Count = count });
                }
            }
        }
    }

    private async Task PublishNewsIfAvailable(IEnumerable<NewsUpdate> newsUpdates, ServiceCollection sc)
    {
        SocketTextChannel channel = sc.DiscordHelper.GetTextChannel(_config.DdNewsChannelId);
        foreach (NewsUpdate update in newsUpdates)
        {
            Log.Information("Publishing news for {Player} - {Score}s",
                update.NewEntry.Username, update.NewEntry.Time / 10_000d);

            await PublishSingleNews(update, channel, sc);

            DdNewsItem newsItem = new()
            {
                LeaderboardId = update.NewEntry.Id,
                OldEntry = update.OldEntry,
                NewEntry = update.NewEntry,
                TimeOfOccurenceUtc = DateTimeOffset.UtcNow,
                Nth = update.Nth,
            };

            sc.DbContext.DdNews.Add(newsItem);
        }
    }

    private async Task PublishSingleNews(NewsUpdate update, SocketTextChannel channel, ServiceCollection sc)
    {
        string message = await CreateNewsMessage(update, sc);
        string? countryCode = await GetCountryCode(update.NewEntry.Id, sc);

        using MemoryStream image = _imageGenerator.CreateImage(
            update.NewEntry.Rank,
            update.NewEntry.Username,
            update.NewEntry.Time,
            countryCode);

        string filename = $"{update.NewEntry.Username}_{update.NewEntry.Time}.png";
        await channel.SendFileAsync(image, filename, message);
    }

    private async Task<string> CreateNewsMessage(NewsUpdate update, ServiceCollection sc)
    {
        string? username = update.NewEntry.Username;

        DdUser? dbUser = await sc.UserRepository.FindAsync(update.NewEntry.Id);
        if (dbUser != null)
        {
            SocketGuildUser? guildUser = sc.DiscordHelper.GetGuildUser(_config.DdPalsId, dbUser.DiscordId);
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

    private static async Task<string?> GetCountryCode(uint playerLbId, ServiceCollection sc)
    {
        try
        {
            return await sc.WebService.GetCountryCodeForplayer(playerLbId);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not fetch country code for player {PlayerId}", playerLbId);
            return null;
        }
    }

    private static async Task<List<GetRecentResponse>> FetchRecentRunsWithBackfillAsync(DateTimeOffset lastCheck, IWebService webService)
    {
        List<GetRecentResponse> allRuns = [];
        HashSet<(uint LeaderboardId, long Timestamp, int Time)> seen = [];
        DateTimeOffset fetchBefore = DateTimeOffset.UtcNow;
        DateTimeOffset previousFetchBefore = DateTimeOffset.MinValue;

        while (true)
        {
            IReadOnlyList<GetRecentResponse> batch = await webService.GetRecentScores(fetchBefore, GetRecentLimit);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (GetRecentResponse run in batch)
            {
                if (seen.Add((run.LeaderboardId, run.Timestamp.ToUnixTimeSeconds(), run.Time)))
                {
                    allRuns.Add(run);
                }
            }

            GetRecentResponse oldestInBatch = batch.MinBy(r => r.Timestamp)!;

            if (batch.Count < GetRecentLimit || oldestInBatch.Timestamp <= lastCheck)
            {
                break;
            }

            int sameTimestampCount = batch.Count(r => r.Timestamp == oldestInBatch.Timestamp);
            if (sameTimestampCount > 1)
            {
                Log.Warning(
                    "Pagination boundary at timestamp {Timestamp} has {Count} entries; some entries may be lost due to API limit of {Limit}",
                    oldestInBatch.Timestamp,
                    sameTimestampCount,
                    GetRecentLimit);
            }

            if (fetchBefore == previousFetchBefore)
            {
                Log.Warning("Pagination loop detected at timestamp {Timestamp}; stopping to avoid infinite loop", oldestInBatch.Timestamp);
                break;
            }

            previousFetchBefore = fetchBefore;
            fetchBefore = oldestInBatch.Timestamp.AddSeconds(1);
        }

        return allRuns;
    }

    private static async Task<DateTimeOffset> GetLastCheckAsync(AppDbContext dbContext)
    {
        NewsCursor? cursor = await dbContext.NewsCursors.FirstOrDefaultAsync();
        if (cursor != null)
        {
            return cursor.LastCheckedAt;
        }

        DateTimeOffset fallback = DateTimeOffset.UtcNow.AddMinutes(-5);
        dbContext.NewsCursors.Add(new NewsCursor { LastCheckedAt = fallback });
        await dbContext.SaveChangesAsync();
        return fallback;
    }

    private static async Task UpdateLastCheckAsync(AppDbContext dbContext, DateTimeOffset value)
    {
        NewsCursor? cursor = await dbContext.NewsCursors.FirstOrDefaultAsync();
        if (cursor != null)
        {
            cursor.LastCheckedAt = value;
        }
        else
        {
            dbContext.NewsCursors.Add(new NewsCursor { LastCheckedAt = value });
        }
    }

    private sealed record ServiceCollection(
        INewsRepository NewsRepository,
        IUserRepository UserRepository,
        AppDbContext DbContext,
        IDiscordHelper DiscordHelper,
        IWebService WebService)
    {
        public ServiceCollection(IServiceScope scope) : this(
            scope.ServiceProvider.GetRequiredService<INewsRepository>(),
            scope.ServiceProvider.GetRequiredService<IUserRepository>(),
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            scope.ServiceProvider.GetRequiredService<IDiscordHelper>(),
            scope.ServiceProvider.GetRequiredService<IWebService>())
        {
        }
    }
}

internal sealed record ProcessRunsResult(
    List<PlayerPb> Upserts,
    List<NewsUpdate> NewsUpdates,
    Dictionary<int, int> HundredthChanges);

internal readonly record struct NewsUpdate(
    EntryResponse OldEntry,
    EntryResponse NewEntry,
    int Nth);
