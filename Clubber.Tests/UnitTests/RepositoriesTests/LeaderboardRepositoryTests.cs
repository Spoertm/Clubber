using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clubber.Tests.UnitTests.RepositoriesTests;

public sealed class LeaderboardRepositoryTests : IDisposable
{
	private readonly SqliteConnection _connection;

	public LeaderboardRepositoryTests()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();
	}

	public void Dispose()
	{
		_connection.Close();
		_connection.Dispose();
	}

	private DbService CreateContext()
	{
		DbContextOptions<DbService> options = new DbContextOptionsBuilder<DbService>()
			.UseSqlite(_connection)
			.Options;

		DbService context = new(options);
		context.Database.EnsureCreated();
		return context;
	}

	#region UpdateBestSplitsAsync

	[Fact]
	public async Task UpdateBestSplitsAsync_EmptyDatabase_AddsAllSplits()
	{
		List<Split> splits =
		[
			new Split("350", 366, 100),
			new Split("700", 709, 200),
		];
		DdStatsFullRunResponse run = CreateDdStatsRun("TestPlayer", 1);

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(BestSplit[] oldSplits, BestSplit[] updatedSplits) = await sut.UpdateBestSplitsAsync(splits, run, "desc");

		Assert.Empty(oldSplits);
		Assert.Equal(2, updatedSplits.Length);
		Assert.Equal(100, updatedSplits.First(s => s.Name == "350").Value);
		Assert.Equal(200, updatedSplits.First(s => s.Name == "700").Value);

		BestSplit[] dbSplits = await db.BestSplits.AsNoTracking().ToArrayAsync();
		Assert.Equal(2, dbSplits.Length);
	}

	[Fact]
	public async Task UpdateBestSplitsAsync_NewSplitBeatsExisting_UpdatesAndReturnsBoth()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.BestSplits.Add(new BestSplit { Name = "350", Time = 366, Value = 50, Description = "old" });
			await seedDb.SaveChangesAsync();
		}

		List<Split> splits = [new Split("350", 366, 100)];
		DdStatsFullRunResponse run = CreateDdStatsRun("TestPlayer", 1);

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(BestSplit[] oldSplits, BestSplit[] updatedSplits) = await sut.UpdateBestSplitsAsync(splits, run, "new desc");

		Assert.Single(oldSplits);
		Assert.Equal(50, oldSplits[0].Value);
		Assert.Single(updatedSplits);
		Assert.Equal(100, updatedSplits[0].Value);

		BestSplit? dbSplit = await db.BestSplits.AsNoTracking().FirstOrDefaultAsync(s => s.Name == "350");
		Assert.NotNull(dbSplit);
		Assert.Equal(100, dbSplit.Value);
		Assert.Equal("new desc", dbSplit.Description);
	}

	[Fact]
	public async Task UpdateBestSplitsAsync_NewSplitWorseThanExisting_DoesNothing()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.BestSplits.Add(new BestSplit { Name = "350", Time = 366, Value = 100, Description = "old" });
			await seedDb.SaveChangesAsync();
		}

		List<Split> splits = [new Split("350", 366, 50)];
		DdStatsFullRunResponse run = CreateDdStatsRun("TestPlayer", 1);

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(BestSplit[] oldSplits, BestSplit[] updatedSplits) = await sut.UpdateBestSplitsAsync(splits, run, "new desc");

		Assert.Single(oldSplits);
		Assert.Equal(100, oldSplits[0].Value);
		Assert.Empty(updatedSplits);

		BestSplit? dbSplit = await db.BestSplits.AsNoTracking().FirstOrDefaultAsync(s => s.Name == "350");
		Assert.NotNull(dbSplit);
		Assert.Equal(100, dbSplit.Value);
		Assert.Equal("old", dbSplit.Description);
	}

	[Fact]
	public async Task UpdateBestSplitsAsync_EqualValue_DoesNothing()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.BestSplits.Add(new BestSplit { Name = "350", Time = 366, Value = 100, Description = "old" });
			await seedDb.SaveChangesAsync();
		}

		List<Split> splits = [new Split("350", 366, 100)];
		DdStatsFullRunResponse run = CreateDdStatsRun("TestPlayer", 1);

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(BestSplit[] oldSplits, BestSplit[] updatedSplits) = await sut.UpdateBestSplitsAsync(splits, run, "new desc");

		Assert.Single(oldSplits);
		Assert.Empty(updatedSplits);
	}

	[Fact]
	public async Task UpdateBestSplitsAsync_MixedResults_OnlyUpdatesBetterOnes()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.BestSplits.AddRange(
				new BestSplit { Name = "350", Time = 366, Value = 100, Description = "old" },
				new BestSplit { Name = "700", Time = 709, Value = 200, Description = "old" }
			);
			await seedDb.SaveChangesAsync();
		}

		List<Split> splits =
		[
			new Split("350", 366, 150),
			new Split("700", 709, 100),
		];
		DdStatsFullRunResponse run = CreateDdStatsRun("TestPlayer", 1);

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(BestSplit[] oldSplits, BestSplit[] updatedSplits) = await sut.UpdateBestSplitsAsync(splits, run, "desc");

		Assert.Equal(2, oldSplits.Length);
		Assert.Single(updatedSplits);
		Assert.Equal("350", updatedSplits[0].Name);
		Assert.Equal(150, updatedSplits[0].Value);

		BestSplit[] dbSplits = await db.BestSplits.AsNoTracking().ToArrayAsync();
		Assert.Equal(2, dbSplits.Length);
		Assert.Equal(150, dbSplits.First(s => s.Name == "350").Value);
		Assert.Equal(200, dbSplits.First(s => s.Name == "700").Value);
	}

	[Fact]
	public async Task UpdateBestSplitsAsync_NewSplitNotInDatabase_AddsIt()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.BestSplits.Add(new BestSplit { Name = "350", Time = 366, Value = 100, Description = "old" });
			await seedDb.SaveChangesAsync();
		}

		List<Split> splits =
		[
			new Split("350", 366, 100),
			new Split("700", 709, 200),
		];
		DdStatsFullRunResponse run = CreateDdStatsRun("TestPlayer", 1);

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(BestSplit[] oldSplits, BestSplit[] updatedSplits) = await sut.UpdateBestSplitsAsync(splits, run, "desc");

		Assert.Single(oldSplits);
		Assert.Single(updatedSplits);
		Assert.Equal("700", updatedSplits[0].Name);

		BestSplit[] dbSplits = await db.BestSplits.AsNoTracking().ToArrayAsync();
		Assert.Equal(2, dbSplits.Length);
	}

	#endregion

	#region UpdateTopHomingPeakAsync

	[Fact]
	public async Task UpdateTopHomingPeakAsync_NewPlayer_AddsAndReturnsRun()
	{
		HomingPeakRun run = new() { PlayerLeaderboardId = 1, PlayerName = "Player1", HomingPeak = 100, Source = "ddstats" };

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(HomingPeakRun? oldRun, HomingPeakRun? newRun) = await sut.UpdateTopHomingPeakAsync(run);

		Assert.Null(oldRun);
		Assert.NotNull(newRun);
		Assert.Equal(100, newRun.HomingPeak);

		HomingPeakRun? dbRun = await db.TopHomingPeaks.AsNoTracking().FirstOrDefaultAsync(r => r.PlayerLeaderboardId == 1);
		Assert.NotNull(dbRun);
		Assert.Equal(100, dbRun.HomingPeak);
	}

	[Fact]
	public async Task UpdateTopHomingPeakAsync_HigherPeak_UpdatesAndReturnsBoth()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.TopHomingPeaks.Add(new HomingPeakRun { PlayerLeaderboardId = 1, PlayerName = "Player1", HomingPeak = 100, Source = "ddstats" });
			await seedDb.SaveChangesAsync();
		}

		HomingPeakRun run = new() { PlayerLeaderboardId = 1, PlayerName = "Player1", HomingPeak = 150, Source = "ddstats" };

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(HomingPeakRun? oldRun, HomingPeakRun? newRun) = await sut.UpdateTopHomingPeakAsync(run);

		Assert.NotNull(oldRun);
		Assert.Equal(100, oldRun.HomingPeak);
		Assert.NotNull(newRun);
		Assert.Equal(150, newRun.HomingPeak);

		HomingPeakRun? dbRun = await db.TopHomingPeaks.AsNoTracking().FirstOrDefaultAsync(r => r.PlayerLeaderboardId == 1);
		Assert.NotNull(dbRun);
		Assert.Equal(150, dbRun.HomingPeak);
	}

	[Fact]
	public async Task UpdateTopHomingPeakAsync_LowerPeak_DoesNothing()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.TopHomingPeaks.Add(new HomingPeakRun { PlayerLeaderboardId = 1, PlayerName = "Player1", HomingPeak = 100, Source = "ddstats" });
			await seedDb.SaveChangesAsync();
		}

		HomingPeakRun run = new() { PlayerLeaderboardId = 1, PlayerName = "Player1", HomingPeak = 50, Source = "ddstats" };

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(HomingPeakRun? oldRun, HomingPeakRun? newRun) = await sut.UpdateTopHomingPeakAsync(run);

		Assert.NotNull(oldRun);
		Assert.Equal(100, oldRun.HomingPeak);
		Assert.Null(newRun);

		HomingPeakRun? dbRun = await db.TopHomingPeaks.AsNoTracking().FirstOrDefaultAsync(r => r.PlayerLeaderboardId == 1);
		Assert.NotNull(dbRun);
		Assert.Equal(100, dbRun.HomingPeak);
	}

	[Fact]
	public async Task UpdateTopHomingPeakAsync_EqualPeak_DoesNothing()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.TopHomingPeaks.Add(new HomingPeakRun { PlayerLeaderboardId = 1, PlayerName = "Player1", HomingPeak = 100, Source = "ddstats" });
			await seedDb.SaveChangesAsync();
		}

		HomingPeakRun run = new() { PlayerLeaderboardId = 1, PlayerName = "Player1", HomingPeak = 100, Source = "ddstats" };

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(HomingPeakRun? oldRun, HomingPeakRun? newRun) = await sut.UpdateTopHomingPeakAsync(run);

		Assert.NotNull(oldRun);
		Assert.Null(newRun);
	}

	[Fact]
	public async Task UpdateTopHomingPeakAsync_DifferentPlayers_TracksSeparately()
	{
		using (DbService seedDb = CreateContext())
		{
			seedDb.TopHomingPeaks.Add(new HomingPeakRun { PlayerLeaderboardId = 1, PlayerName = "Player1", HomingPeak = 100, Source = "ddstats" });
			await seedDb.SaveChangesAsync();
		}

		HomingPeakRun run = new() { PlayerLeaderboardId = 2, PlayerName = "Player2", HomingPeak = 150, Source = "ddstats" };

		using DbService db = CreateContext();
		LeaderboardRepository sut = new(db);

		(HomingPeakRun? oldRun, HomingPeakRun? newRun) = await sut.UpdateTopHomingPeakAsync(run);

		Assert.Null(oldRun);
		Assert.NotNull(newRun);

		HomingPeakRun[] dbRuns = await db.TopHomingPeaks.AsNoTracking().ToArrayAsync();
		Assert.Equal(2, dbRuns.Length);
	}

	#endregion

	private static DdStatsFullRunResponse CreateDdStatsRun(string playerName, int playerId)
	{
		return new DdStatsFullRunResponse
		{
			GameInfo = new GameInfo
			{
				PlayerName = playerName,
				PlayerId = playerId,
			},
			States = [],
		};
	}
}
