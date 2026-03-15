using Clubber.Domain.Models.Responses;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clubber.Tests.UnitTests.RepositoriesTests;

public sealed class NewsRepositoryTests : IDisposable
{
	private readonly SqliteConnection _connection;

	public NewsRepositoryTests()
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

	#region AddAsync

	[Fact]
	public async Task AddAsync_AddsItemToDatabase()
	{
		DdNewsItem item = CreateDdNewsItem(leaderboardId: 1, nth: 1);

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		await sut.AddAsync(item);

		DdNewsItem? dbItem = await db.DdNews.AsNoTracking().FirstOrDefaultAsync();
		Assert.NotNull(dbItem);
		Assert.Equal(1, dbItem.LeaderboardId);
		Assert.Equal(1, dbItem.Nth);
	}

	[Fact]
	public async Task AddAsync_MultipleItems_AddsAllItems()
	{
		DdNewsItem item1 = CreateDdNewsItem(leaderboardId: 1, nth: 1);
		DdNewsItem item2 = CreateDdNewsItem(leaderboardId: 2, nth: 2);

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		await sut.AddAsync(item1);
		await sut.AddAsync(item2);

		DdNewsItem[] dbItems = await db.DdNews.AsNoTracking().ToArrayAsync();
		Assert.Equal(2, dbItems.Length);
	}

	[Fact]
	public async Task AddAsync_SetsAutoIncrementedItemId()
	{
		DdNewsItem item1 = CreateDdNewsItem(leaderboardId: 1, nth: 1);
		DdNewsItem item2 = CreateDdNewsItem(leaderboardId: 2, nth: 2);

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		await sut.AddAsync(item1);
		await sut.AddAsync(item2);

		DdNewsItem[] dbItems = await db.DdNews.AsNoTracking().OrderBy(d => d.ItemId).ToArrayAsync();
		Assert.Equal(1, dbItems[0].ItemId);
		Assert.Equal(2, dbItems[1].ItemId);
	}

	#endregion

	#region GetRecentAsync

	[Fact]
	public async Task GetRecentAsync_EmptyDatabase_ReturnsEmptyArray()
	{
		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		DdNewsItem[] result = await sut.GetRecentAsync();

		Assert.Empty(result);
	}

	[Fact]
	public async Task GetRecentAsync_ReturnsItemsOrderedByTimeDescending()
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;
		using (DbService seedDb = CreateContext())
		{
			seedDb.DdNews.AddRange(
				CreateDdNewsItem(leaderboardId: 1, nth: 1, timeOfOccurence: now.AddHours(-2)),
				CreateDdNewsItem(leaderboardId: 2, nth: 2, timeOfOccurence: now),
				CreateDdNewsItem(leaderboardId: 3, nth: 3, timeOfOccurence: now.AddHours(-1))
			);
			await seedDb.SaveChangesAsync();
		}

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		DdNewsItem[] result = await sut.GetRecentAsync();

		Assert.Equal(3, result.Length);
		Assert.Equal(2, result[0].LeaderboardId);
		Assert.Equal(3, result[1].LeaderboardId);
		Assert.Equal(1, result[2].LeaderboardId);
	}

	[Fact]
	public async Task GetRecentAsync_DoesNotTrackEntities()
	{
		DdNewsItem item = CreateDdNewsItem(leaderboardId: 1, nth: 1);
		using (DbService seedDb = CreateContext())
		{
			seedDb.DdNews.Add(item);
			await seedDb.SaveChangesAsync();
		}

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		await sut.GetRecentAsync();

		Assert.Empty(db.ChangeTracker.Entries());
	}

	#endregion

	#region RemoveOlderThanAsync

	[Fact]
	public async Task RemoveOlderThanAsync_NoItemsOlderThanCutoff_DoesNotRemoveAnything()
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;
		using (DbService seedDb = CreateContext())
		{
			seedDb.DdNews.Add(CreateDdNewsItem(leaderboardId: 1, nth: 1, timeOfOccurence: now.AddHours(-1)));
			await seedDb.SaveChangesAsync();
		}

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		await sut.RemoveOlderThanAsync(TimeSpan.FromHours(2));

		DdNewsItem[] dbItems = await db.DdNews.AsNoTracking().ToArrayAsync();
		Assert.Single(dbItems);
	}

	[Fact]
	public async Task RemoveOlderThanAsync_ItemsOlderThanCutoff_RemovesOldItems()
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;
		using (DbService seedDb = CreateContext())
		{
			seedDb.DdNews.AddRange(
				CreateDdNewsItem(leaderboardId: 1, nth: 1, timeOfOccurence: now.AddHours(-3)),
				CreateDdNewsItem(leaderboardId: 2, nth: 2, timeOfOccurence: now.AddHours(-1))
			);
			await seedDb.SaveChangesAsync();
		}

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		await sut.RemoveOlderThanAsync(TimeSpan.FromHours(2));

		DdNewsItem[] dbItems = await db.DdNews.AsNoTracking().ToArrayAsync();
		Assert.Single(dbItems);
		Assert.Equal(2, dbItems[0].LeaderboardId);
	}

	[Fact]
	public async Task RemoveOlderThanAsync_AllItemsOlderThanCutoff_RemovesAllItems()
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;
		using (DbService seedDb = CreateContext())
		{
			seedDb.DdNews.AddRange(
				CreateDdNewsItem(leaderboardId: 1, nth: 1, timeOfOccurence: now.AddDays(-2)),
				CreateDdNewsItem(leaderboardId: 2, nth: 2, timeOfOccurence: now.AddDays(-3))
			);
			await seedDb.SaveChangesAsync();
		}

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		await sut.RemoveOlderThanAsync(TimeSpan.FromDays(1));

		DdNewsItem[] dbItems = await db.DdNews.AsNoTracking().ToArrayAsync();
		Assert.Empty(dbItems);
	}

	[Fact]
	public async Task RemoveOlderThanAsync_ItemExactlyAtCutoff_RemovesItem()
	{
		DateTimeOffset now = DateTimeOffset.UtcNow;
		using (DbService seedDb = CreateContext())
		{
			seedDb.DdNews.Add(CreateDdNewsItem(leaderboardId: 1, nth: 1, timeOfOccurence: now.AddHours(-2)));
			await seedDb.SaveChangesAsync();
		}

		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		await sut.RemoveOlderThanAsync(TimeSpan.FromHours(2));

		DdNewsItem[] dbItems = await db.DdNews.AsNoTracking().ToArrayAsync();
		Assert.Empty(dbItems);
	}

	[Fact]
	public async Task RemoveOlderThanAsync_EmptyDatabase_DoesNotThrow()
	{
		using DbService db = CreateContext();
		NewsRepository sut = new(db);

		Exception? exception = await Record.ExceptionAsync(() => sut.RemoveOlderThanAsync(TimeSpan.FromDays(1)));

		Assert.Null(exception);
	}

	#endregion

	private static DdNewsItem CreateDdNewsItem(int leaderboardId, int nth, DateTimeOffset? timeOfOccurence = null)
	{
		return new DdNewsItem(
			LeaderboardId: leaderboardId,
			OldEntry: new EntryResponse { Id = leaderboardId, Username = $"Player{leaderboardId}" },
			NewEntry: new EntryResponse { Id = leaderboardId, Username = $"Player{leaderboardId}" },
			TimeOfOccurenceUtc: timeOfOccurence ?? DateTimeOffset.UtcNow,
			Nth: nth
		);
	}
}
