using Clubber.Discord.Services;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Models.Responses;
using Xunit;

namespace Clubber.Tests.UnitTests.ServicesTests;

public sealed class DdNewsPostServiceTests
{
    [Fact]
    public void ProcessRuns_NoImprovements_ReturnsEmptyResult()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 500_0000),
        ];
        Dictionary<uint, PlayerPb> existingPbs = new()
        {
            [1] = CreatePb(1, 600_0000),
        };
        Dictionary<int, int> hundredthCounts = [];

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Empty(result.Upserts);
        Assert.Empty(result.NewsUpdates);
        Assert.Empty(result.HundredthChanges);
    }

    [Fact]
    public void ProcessRuns_NewPlayer_CreatesUpsertButNoNews()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 750_0000),
        ];
        Dictionary<uint, PlayerPb> existingPbs = [];
        Dictionary<int, int> hundredthCounts = [];

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Single(result.Upserts);
        Assert.Equal(750_0000, result.Upserts[0].Time);
        Assert.Empty(result.NewsUpdates);
    }

    [Fact]
    public void ProcessRuns_NewPlayerBelow700_Skipped()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 500_0000),
        ];
        Dictionary<uint, PlayerPb> existingPbs = [];
        Dictionary<int, int> hundredthCounts = [];

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Empty(result.Upserts);
        Assert.Empty(result.NewsUpdates);
        Assert.Empty(result.HundredthChanges);
    }

    [Fact]
    public void ProcessRuns_ExistingPlayerCrossing700_UpdatesAndTracksHundredth()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 750_0000),
        ];
        Dictionary<uint, PlayerPb> existingPbs = new()
        {
            [1] = CreatePb(1, 650_0000),
        };
        Dictionary<int, int> hundredthCounts = [];

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Single(result.Upserts);
        Assert.Equal(750_0000, result.Upserts[0].Time);
        Assert.Empty(result.NewsUpdates);
        Assert.Single(result.HundredthChanges);
        Assert.Equal(1, result.HundredthChanges[700]);
    }

    [Fact]
    public void ProcessRuns_NewsWorthyImprovement_GeneratesNewsWithCorrectNth()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 1100_0000),
        ];
        Dictionary<uint, PlayerPb> existingPbs = new()
        {
            [1] = CreatePb(1, 1050_0000),
        };
        Dictionary<int, int> hundredthCounts = new() { [1100] = 4 };

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Single(result.Upserts);
        Assert.Single(result.NewsUpdates);
        Assert.Equal(5, result.NewsUpdates[0].Nth);
        Assert.Equal(1100_0000, result.NewsUpdates[0].NewEntry.Time);
        Assert.Equal(1050_0000, result.NewsUpdates[0].OldEntry.Time);
    }

    [Fact]
    public void ProcessRuns_NonNewsWorthyImprovement_UpsertsWithoutNews()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 950_0000),
        ];
        Dictionary<uint, PlayerPb> existingPbs = new()
        {
            [1] = CreatePb(1, 900_0000),
        };
        Dictionary<int, int> hundredthCounts = [];

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Single(result.Upserts);
        Assert.Empty(result.NewsUpdates);
    }

    [Fact]
    public void ProcessRuns_MultiplePlayersCrossingSameHundredth_AssignsSequentialNth()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 1100_0000, timestamp: DateTimeOffset.UtcNow.AddSeconds(1)),
            CreateRun(2, 1100_5000, timestamp: DateTimeOffset.UtcNow.AddSeconds(2)),
        ];
        Dictionary<uint, PlayerPb> existingPbs = new()
        {
            [1] = CreatePb(1, 1050_0000),
            [2] = CreatePb(2, 1090_0000),
        };
        Dictionary<int, int> hundredthCounts = new() { [1100] = 3 };

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Equal(2, result.NewsUpdates.Count);
        Assert.Equal(4, result.NewsUpdates[0].Nth);
        Assert.Equal(5, result.NewsUpdates[1].Nth);
    }

    [Fact]
    public void ProcessRuns_PlayerCrossingMultipleHundredths_IncrementsEachThreshold()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 1200_0000),
        ];
        Dictionary<uint, PlayerPb> existingPbs = new()
        {
            [1] = CreatePb(1, 1090_0000),
        };
        Dictionary<int, int> hundredthCounts = [];

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Single(result.NewsUpdates);
        Assert.Equal(1, result.NewsUpdates[0].Nth);
        Assert.Equal(2, result.HundredthChanges.Count);
        Assert.Equal(1, result.HundredthChanges[1100]);
        Assert.Equal(1, result.HundredthChanges[1200]);
    }

    [Fact]
    public void ProcessRuns_SamePlayerTwiceInBatch_UsesLatestPbForDedup()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        GetRecentResponse[] runs =
        [
            CreateRun(1, 1100_0000, timestamp: now.AddSeconds(1)),
            CreateRun(1, 1150_0000, timestamp: now.AddSeconds(2)),
        ];
        Dictionary<uint, PlayerPb> existingPbs = new()
        {
            [1] = CreatePb(1, 1050_0000),
        };
        Dictionary<int, int> hundredthCounts = new() { [1100] = 0 };

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Single(result.Upserts);
        Assert.Equal(1150_0000, result.Upserts[0].Time);
        Assert.Single(result.NewsUpdates);
        Assert.Equal(1100_0000, result.NewsUpdates[0].NewEntry.Time);
    }

    [Fact]
    public void ProcessRuns_DuplicateRunWithSameTime_Skipped()
    {
        GetRecentResponse[] runs =
        [
            CreateRun(1, 1100_0000),
            CreateRun(1, 1100_0000),
        ];
        Dictionary<uint, PlayerPb> existingPbs = new()
        {
            [1] = CreatePb(1, 1050_0000),
        };
        Dictionary<int, int> hundredthCounts = new() { [1100] = 0 };

        ProcessRunsResult result = DdNewsPostService.ProcessRuns(runs, existingPbs, hundredthCounts);

        Assert.Single(result.Upserts);
        Assert.Single(result.NewsUpdates);
    }

    private static GetRecentResponse CreateRun(uint leaderboardId, int time, DateTimeOffset? timestamp = null)
    {
        return new GetRecentResponse
        {
            LeaderboardId = leaderboardId,
            UserName = $"Player{leaderboardId}",
            Time = time,
            TimestampUnix = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds(),
            Country = "US",
        };
    }

    private static PlayerPb CreatePb(uint leaderboardId, int time)
    {
        return new PlayerPb
        {
            LeaderboardId = leaderboardId,
            Username = $"Player{leaderboardId}",
            Time = time,
            Rank = 100,
            LastUpdated = DateTimeOffset.UtcNow,
        };
    }
}
