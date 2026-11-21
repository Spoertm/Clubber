using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Clubber.Tests.UnitTests.ServicesTests;

public sealed class UserStatsServiceTests
{
    private const ulong DiscordId = 42;
    private const uint LeaderboardId = 7;

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IWebService _webService = Substitute.For<IWebService>();
    private readonly UserStatsService _sut;

    public UserStatsServiceTests()
    {
        IOptions<AppConfig> options = Options.Create(TestData.CreateAppConfig());
        _sut = new UserStatsService(_userRepository, new UserService(options, _userRepository), _webService);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsValidationFailure_WhenUserIsNotRegistered()
    {
        IGuildUser user = CreateUser();
        _userRepository.FindAsync(DiscordId).Returns(Task.FromResult<DdUser?>(null));
        _userRepository.DiscordIdExistsAsync(DiscordId).Returns(false);

        Result<StatsOutcome> result = await _sut.GetStatsAsync(user, requestingUserIsStatsUser: true, full: false);

        Failure failure = Assert.IsType<Failure>(result.Value);
        Assert.Contains("not registered", failure.Message, StringComparison.OrdinalIgnoreCase);
        await _webService.DidNotReceive().GetLbPlayers(Arg.Any<IEnumerable<uint>>());
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsNotRegistered_WhenValidationPassesButNoRowExists()
    {
        IGuildUser user = CreateUser();
        _userRepository.FindAsync(DiscordId).Returns(Task.FromResult<DdUser?>(null));
        _userRepository.DiscordIdExistsAsync(DiscordId).Returns(true);

        Result<StatsOutcome> result = await _sut.GetStatsAsync(user, requestingUserIsStatsUser: true, full: false);

        Failure failure = Assert.IsType<Failure>(result.Value);
        Assert.Equal("That user isn't registered.", failure.Message);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsStatsEmbedWithFullStatsButton_WhenNotFull()
    {
        IGuildUser user = CreateUser();
        SetUpRegisteredPlayer();

        Result<StatsOutcome> result = await _sut.GetStatsAsync(user, requestingUserIsStatsUser: true, full: false);

        StatsOutcome outcome = Assert.IsType<Success<StatsOutcome>>(result.Value).Value;
        Assert.Contains("Stats for player", outcome.Embed.Title);
        ActionRowComponent row = Assert.IsType<ActionRowComponent>(Assert.Single(outcome.Components!.Components));
        ButtonComponent button = Assert.IsType<ButtonComponent>(Assert.Single(row.Components));
        Assert.Equal($"stats:{DiscordId}:{LeaderboardId}", button.CustomId);
        await _webService.Received(1).GetLbPlayers(
            Arg.Is<IEnumerable<uint>>(ids => ids.SequenceEqual(new uint[] { LeaderboardId })));
    }

    [Fact]
    public async Task GetStatsAsync_OmitsFullStatsButton_WhenFull()
    {
        IGuildUser user = CreateUser();
        SetUpRegisteredPlayer();

        Result<StatsOutcome> result = await _sut.GetStatsAsync(user, requestingUserIsStatsUser: true, full: true);

        StatsOutcome outcome = Assert.IsType<Success<StatsOutcome>>(result.Value).Value;
        Assert.NotNull(outcome.Embed);
        Assert.Null(outcome.Components);
    }

    private static IGuildUser CreateUser() => TestData.CreateGuildUser(DiscordId, "player", []);

    private void SetUpRegisteredPlayer()
    {
        _userRepository.FindAsync(DiscordId).Returns(Task.FromResult<DdUser?>(new DdUser(DiscordId, LeaderboardId)));
        _webService.GetLbPlayers(Arg.Any<IEnumerable<uint>>())
            .Returns(Task.FromResult<IReadOnlyList<EntryResponse>>([CreateEntry()]));
        _webService.GetPlayerHistory(LeaderboardId).Returns(Task.FromResult<GetPlayerHistory?>(null));
    }

    private static EntryResponse CreateEntry() => new()
    {
        Id = LeaderboardId,
        Username = "player",
        Rank = 1,
        Time = 1300 * 10_000,
        DeathType = 0,
        DaggersFired = 100,
        DaggersHit = 50,
    };
}
