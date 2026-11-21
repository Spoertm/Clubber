using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Data;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Clubber.Tests.UnitTests.ServicesTests;

public sealed class RoleUpdateServiceTests : IDisposable
{
    private const ulong DiscordId = 42;
    private const uint LeaderboardId = 7;
    private const ulong TopRoleId = 1300;
    private const ulong LowerRoleId = 1200;

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IWebService _webService = Substitute.For<IWebService>();
    private readonly RoleUpdateService _sut;

    public RoleUpdateServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        IOptions<AppConfig> options = Options.Create(TestData.CreateAppConfig());
        _dbContext = CreateContext();

        UserService userService = new(options, _userRepository);
        RoleConfigService roleConfigService = new(_dbContext, new MemoryCache(new MemoryCacheOptions()));
        ScoreRoleService scoreRoleService = new(options, _webService, _userRepository, roleConfigService);

        _sut = new RoleUpdateService(userService, scoreRoleService);

        _webService.GetWorldRecords().Returns(Task.FromResult(new GetWorldRecordDataContainer
        {
            WorldRecordHolders = [],
            WorldRecords = [],
        }));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task PrepareAsync_ReturnsValidationFailure_WhenUserIsNotRegistered()
    {
        IGuildUser user = CreateUser([]);
        _userRepository.DiscordIdExistsAsync(DiscordId).Returns(false);

        Result<RoleUpdateOutcome> result = await _sut.PrepareAsync(user);

        Failure failure = Assert.IsType<Failure>(result.Value);
        Assert.Contains("not registered", failure.Message, StringComparison.OrdinalIgnoreCase);
        await user.DidNotReceive().AddRolesAsync(Arg.Any<IEnumerable<ulong>>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task PrepareAsync_ReturnsRoleChangeFailure_WhenPlayerIsNotInDatabase()
    {
        IGuildUser user = CreateUser([]);
        _userRepository.DiscordIdExistsAsync(DiscordId).Returns(true);
        _userRepository.FindAsync(DiscordId).Returns(Task.FromResult<DdUser?>(null));

        Result<RoleUpdateOutcome> result = await _sut.PrepareAsync(user);

        Failure failure = Assert.IsType<Failure>(result.Value);
        Assert.Equal("User is not registered.", failure.Message);
    }

    [Fact]
    public async Task PrepareAsync_AppliesRolesAndReturnsEmbed_WhenRolesChange()
    {
        IGuildUser user = CreateUser([]);
        SetUpRegisteredPlayer(seconds: 1300);

        Result<RoleUpdateOutcome> result = await _sut.PrepareAsync(user);

        RoleUpdateOutcome outcome = Assert.IsType<Success<RoleUpdateOutcome>>(result.Value).Value;
        Assert.NotNull(outcome.Embed);
        Assert.Null(outcome.Text);
        await user.Received(1).AddRolesAsync(
            Arg.Is<IEnumerable<ulong>>(roleIds => roleIds.SequenceEqual(new ulong[] { TopRoleId })),
            Arg.Any<RequestOptions>());
        await user.DidNotReceive().RemoveRolesAsync(Arg.Any<IEnumerable<ulong>>(), Arg.Any<RequestOptions>());
        await _webService.Received(1).GetLbPlayers(
            Arg.Is<IEnumerable<uint>>(ids => ids.SequenceEqual(new uint[] { LeaderboardId })));
    }

    [Fact]
    public async Task PrepareAsync_SwapsLowerRoleForTopRole_WhenUserEarnsTopScore()
    {
        IGuildUser user = CreateUser([LowerRoleId]);
        SetUpRegisteredPlayer(seconds: 1300);

        Result<RoleUpdateOutcome> result = await _sut.PrepareAsync(user);

        RoleUpdateOutcome outcome = Assert.IsType<Success<RoleUpdateOutcome>>(result.Value).Value;
        Assert.NotNull(outcome.Embed);
        await user.Received(1).AddRolesAsync(
            Arg.Is<IEnumerable<ulong>>(roleIds => roleIds.SequenceEqual(new ulong[] { TopRoleId })),
            Arg.Any<RequestOptions>());
        await user.Received(1).RemoveRolesAsync(
            Arg.Is<IEnumerable<ulong>>(roleIds => roleIds.SequenceEqual(new ulong[] { LowerRoleId })),
            Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task PrepareAsync_ReportsHighestRole_WhenUserAlreadyHasTopRole()
    {
        IGuildUser user = CreateUser([TopRoleId]);
        SetUpRegisteredPlayer(seconds: 1300);

        Result<RoleUpdateOutcome> result = await _sut.PrepareAsync(user);

        RoleUpdateOutcome outcome = Assert.IsType<Success<RoleUpdateOutcome>>(result.Value).Value;
        Assert.Null(outcome.Embed);
        Assert.Contains("already have the highest role", outcome.Text);
        await user.DidNotReceive().AddRolesAsync(Arg.Any<IEnumerable<ulong>>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task PrepareAsync_ReportsNextMilestone_WhenUserHasLowerRole()
    {
        IGuildUser user = CreateUser([LowerRoleId]);
        SetUpRegisteredPlayer(seconds: 1200);

        Result<RoleUpdateOutcome> result = await _sut.PrepareAsync(user);

        RoleUpdateOutcome outcome = Assert.IsType<Success<RoleUpdateOutcome>>(result.Value).Value;
        Assert.Null(outcome.Embed);
        Assert.Contains("away from the next role", outcome.Text);
        Assert.Matches(@"\*\*100[.,]0000s\*\*", outcome.Text);
        Assert.Contains(MentionUtils.MentionRole(TopRoleId), outcome.Text);
    }

    private static IGuildUser CreateUser(IReadOnlyCollection<ulong> roleIds) =>
        TestData.CreateGuildUser(DiscordId, "player", roleIds);

    private void SetUpRegisteredPlayer(int seconds)
    {
        _userRepository.DiscordIdExistsAsync(DiscordId).Returns(true);
        _userRepository.FindAsync(DiscordId).Returns(Task.FromResult<DdUser?>(new DdUser(DiscordId, LeaderboardId)));
        _webService.GetLbPlayers(Arg.Any<IEnumerable<uint>>())
            .Returns(Task.FromResult<IReadOnlyList<EntryResponse>>([CreateEntry(seconds)]));
    }

    private static EntryResponse CreateEntry(int seconds) => new()
    {
        Id = LeaderboardId,
        Username = "player",
        Rank = 1,
        Time = seconds * 10_000,
        DeathType = 0,
        DaggersFired = 100,
        DaggersHit = 50,
    };

    private AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        AppDbContext context = new(options);
        context.Database.EnsureCreated();
        context.ScoreRoles.AddRange(
            new ScoreRole { Id = 1300, DiscordRoleId = TopRoleId },
            new ScoreRole { Id = 1200, DiscordRoleId = LowerRoleId });
        context.SaveChanges();
        return context;
    }
}
