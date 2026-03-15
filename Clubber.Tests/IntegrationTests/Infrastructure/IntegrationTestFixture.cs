using Clubber.Domain.Configuration;
using Clubber.Domain.Repositories;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Services;
using Discord;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Clubber.Tests.IntegrationTests.Infrastructure;

/// <summary>
/// Test fixture providing a real InMemory database and mocked services for integration testing.
/// </summary>
public sealed class IntegrationTestFixture : IDisposable
{
	public ServiceProvider ServiceProvider { get; }
	public DbService DbContext { get; }
	public IWebService WebService { get; }
	private readonly SqliteConnection _connection;

	public IntegrationTestFixture()
	{
		ServiceCollection services = new();

		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		services.AddDbContext<DbService>(options => options.UseSqlite(_connection));
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddSingleton(_ => CreateMockWebService());
		services.Configure<AppConfig>(cfg =>
		{
			cfg.UnregisteredRoleId = 999999;
			cfg.DdPalsId = 111111;
			cfg.DailyUpdateChannelId = 222222;
			cfg.DailyUpdateLoggingChannelId = 333333;
		});

		ServiceProvider = services.BuildServiceProvider();
		DbContext = ServiceProvider.GetRequiredService<DbService>();
		WebService = ServiceProvider.GetRequiredService<IWebService>();

		DbContext.Database.EnsureCreated();
	}

	/// <summary>
	/// Creates a mock IWebService with default world records data.
	/// Tests can override specific methods as needed.
	/// </summary>
	private static IWebService CreateMockWebService()
	{
		IWebService mock = Substitute.For<IWebService>();

		mock.GetWorldRecords()
			.Returns(Task.FromResult(new GetWorldRecordDataContainer
			{
				WorldRecordHolders = [],
				WorldRecords = []
			}));

		mock.GetLbPlayers(Arg.Any<IEnumerable<uint>>())
			.Returns(Task.FromResult<IReadOnlyList<EntryResponse>>([]));

		return mock;
	}

	/// <summary>
	/// Seeds the database with registered users for testing.
	/// </summary>
	public async Task SeedUsersAsync(params DdUser[] users)
	{
		await DbContext.DdPlayers.AddRangeAsync(users);
		await DbContext.SaveChangesAsync();
	}

	/// <summary>
	/// Configures the mock WebService to return specific leaderboard entries.
	/// </summary>
	public void SetupLeaderboardResponse(params EntryResponse[] entries)
	{
		WebService.GetLbPlayers(Arg.Any<IEnumerable<uint>>())
			.Returns(Task.FromResult<IReadOnlyList<EntryResponse>>(entries.ToList()));
	}

	/// <summary>
	/// Configures the mock WebService to return specific world record holders.
	/// </summary>
	public void SetupWorldRecords(params int[] formerWrPlayerIds)
	{
		WebService.GetWorldRecords().Returns(Task.FromResult(new GetWorldRecordDataContainer
		{
			WorldRecordHolders = formerWrPlayerIds.Select(id => new GetWorldRecordHolder
			{
				Id = id,
				Usernames = [$"Player{id}"],
				TotalTimeHeld = TimeSpan.Zero,
				LongestTimeHeldConsecutively = TimeSpan.Zero,
				WorldRecordCount = 1,
				FirstHeld = DateTime.UtcNow,
				LastHeld = DateTime.UtcNow,
				MostRecentUsername = $"Player{id}"
			}).ToList(),
			WorldRecords = []
		}));
	}

	/// <summary>
	/// Creates a mock IGuildUser for testing.
	/// </summary>
	public static IGuildUser CreateMockGuildUser(ulong id, string username, params ulong[] roleIds)
	{
		IGuildUser user = Substitute.For<IGuildUser>();
		user.Id.Returns(id);
		user.Username.Returns(username);
		user.GuildPermissions.Returns(GuildPermissions.None);
		user.RoleIds.Returns(roleIds);
		return user;
	}

	/// <summary>
	/// Creates a leaderboard entry for testing.
	/// </summary>
	public static EntryResponse CreateLeaderboardEntry(int id, int timeInSeconds, int rank = 100)
	{
		return new EntryResponse
		{
			Id = id,
			Time = timeInSeconds * 10_000,
			Rank = rank,
			Username = $"Player{id}",
			Kills = 100,
			Gems = 50,
			DaggersHit = 500,
			DaggersFired = 1000,
			DeathType = 0,
			TimeTotal = 1_000_000,
			KillsTotal = 10_000,
			GemsTotal = 5_000,
			DaggersHitTotal = 50_000,
			DaggersFiredTotal = 100_000,
			DeathsTotal = 1000
		};
	}

	public void Dispose()
	{
		DbContext.Dispose();
		ServiceProvider.Dispose();
		_connection.Close();
		_connection.Dispose();
	}
}

/// <summary>
/// Collection fixture to ensure tests don't run in parallel when sharing state.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
