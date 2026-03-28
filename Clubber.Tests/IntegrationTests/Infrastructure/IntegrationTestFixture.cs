using Clubber.Domain.Configuration;
using Clubber.Domain.Data;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Repositories;
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
    public AppDbContext DbContext { get; }
    public IWebService WebService { get; }
    private readonly SqliteConnection _connection;

    public IntegrationTestFixture()
    {
        ServiceCollection services = new();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddMemoryCache();
        services.AddScoped<RoleConfigService>();
        services.AddSingleton(_ => CreateMockWebService());
        services.Configure<AppConfig>(cfg =>
        {
            cfg.UnregisteredRoleId = 999999;
            cfg.DdPalsId = 111111;
            cfg.DailyUpdateChannelId = 222222;
            cfg.DailyUpdateLoggingChannelId = 333333;
        });

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
        WebService = ServiceProvider.GetRequiredService<IWebService>();

        DbContext.Database.EnsureCreated();
        SeedRoleData();
    }

    /// <summary>
    /// Seeds ScoreRoles and RankRoles for testing. Matches AppConfig static data.
    /// </summary>
    private void SeedRoleData()
    {
        // Only seed if not already present
        if (!DbContext.ScoreRoles.Any())
        {
            DbContext.ScoreRoles.AddRange(
                new ScoreRole { Id = 1300, DiscordRoleId = 1300 },
                new ScoreRole { Id = 1295, DiscordRoleId = 1295 },
                new ScoreRole { Id = 1290, DiscordRoleId = 1290 },
                new ScoreRole { Id = 1285, DiscordRoleId = 1285 },
                new ScoreRole { Id = 1280, DiscordRoleId = 1280 },
                new ScoreRole { Id = 1275, DiscordRoleId = 1275 },
                new ScoreRole { Id = 1270, DiscordRoleId = 1270 },
                new ScoreRole { Id = 1265, DiscordRoleId = 1265 },
                new ScoreRole { Id = 1260, DiscordRoleId = 1260 },
                new ScoreRole { Id = 1255, DiscordRoleId = 1255 },
                new ScoreRole { Id = 1250, DiscordRoleId = 1250 },
                new ScoreRole { Id = 1240, DiscordRoleId = 1240 },
                new ScoreRole { Id = 1230, DiscordRoleId = 1230 },
                new ScoreRole { Id = 1220, DiscordRoleId = 1220 },
                new ScoreRole { Id = 1210, DiscordRoleId = 1210 },
                new ScoreRole { Id = 1200, DiscordRoleId = 1200 },
                new ScoreRole { Id = 1190, DiscordRoleId = 1190 },
                new ScoreRole { Id = 1180, DiscordRoleId = 1180 },
                new ScoreRole { Id = 1170, DiscordRoleId = 1170 },
                new ScoreRole { Id = 1160, DiscordRoleId = 1160 },
                new ScoreRole { Id = 1150, DiscordRoleId = 1150 },
                new ScoreRole { Id = 1140, DiscordRoleId = 1140 },
                new ScoreRole { Id = 1130, DiscordRoleId = 1130 },
                new ScoreRole { Id = 1120, DiscordRoleId = 1120 },
                new ScoreRole { Id = 1110, DiscordRoleId = 1110 },
                new ScoreRole { Id = 1100, DiscordRoleId = 1100 },
                new ScoreRole { Id = 1075, DiscordRoleId = 1075 },
                new ScoreRole { Id = 1050, DiscordRoleId = 1050 },
                new ScoreRole { Id = 1025, DiscordRoleId = 1025 },
                new ScoreRole { Id = 1000, DiscordRoleId = 1000 },
                new ScoreRole { Id = 950, DiscordRoleId = 950 },
                new ScoreRole { Id = 900, DiscordRoleId = 900 },
                new ScoreRole { Id = 800, DiscordRoleId = 800 },
                new ScoreRole { Id = 700, DiscordRoleId = 700 },
                new ScoreRole { Id = 600, DiscordRoleId = 600 },
                new ScoreRole { Id = 500, DiscordRoleId = 500 },
                new ScoreRole { Id = 400, DiscordRoleId = 400 },
                new ScoreRole { Id = 300, DiscordRoleId = 300 },
                new ScoreRole { Id = 200, DiscordRoleId = 200 },
                new ScoreRole { Id = 100, DiscordRoleId = 100 },
                new ScoreRole { Id = 0, DiscordRoleId = 0 });
        }

        if (!DbContext.RankRoles.Any())
        {
            DbContext.RankRoles.AddRange(
                new RankRole { Id = 1, DiscordRoleId = 1 },
                new RankRole { Id = 3, DiscordRoleId = 3 },
                new RankRole { Id = 10, DiscordRoleId = 10 },
                new RankRole { Id = 25, DiscordRoleId = 25 });
        }

        DbContext.SaveChanges();
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
