using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Models;
using Clubber.Domain.Repositories;
using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Clubber.Tests.UnitTests.ServicesTests;

public sealed class UserServiceTests
{
    private readonly UserService _sut;
    private readonly IUserRepository _userRepositoryMock = Substitute.For<IUserRepository>();
    private const ulong CheaterRoleId = 666;
    private const ulong ExampleDiscordId = 0;

    public UserServiceTests()
    {
        IConfiguration configMock = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Testing.json")
            .Build();

        AppConfig appConfig = configMock.GetSection("BotConfig").Get<AppConfig>()!;
        IOptions<AppConfig> options = Options.Create(appConfig);

        _sut = new UserService(options, _userRepositoryMock);
    }

    [Theory]
    [InlineData(true, new ulong[] { 1, 2, 3 })]
    [InlineData(true, new ulong[] { 1, 2, 3, CheaterRoleId })]
    [InlineData(false, new ulong[] { 1, 2, 3, CheaterRoleId })]
    public async Task IsValidForRegistration_DetectsBotAndOrCheater_ReturnsError(bool isBot, IReadOnlyCollection<ulong> roleIds)
    {
        IGuildUser guildUser = Substitute.For<IGuildUser>();
        guildUser.IsBot.Returns(isBot);
        guildUser.RoleIds.Returns(roleIds);

        Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser, 1, true);
        Assert.True(isValidForRegistrationResponse.IsFailure);
    }

    [Theory]
    [InlineData(true, new ulong[] { 1, 2, 3 })]
    [InlineData(true, new ulong[] { 1, 2, 3, CheaterRoleId })]
    [InlineData(false, new ulong[] { 1, 2, 3, CheaterRoleId })]
    public async Task IsValid_DetectsBotAndOrCheater_ReturnsError(bool isBot, IReadOnlyCollection<ulong> roleIds)
    {
        IGuildUser guildUser = Substitute.For<IGuildUser>();
        guildUser.IsBot.Returns(isBot);
        guildUser.RoleIds.Returns(roleIds);

        Result isValidResponse = await _sut.IsValid(guildUser, true);
        Assert.True(isValidResponse.IsFailure);
    }

    [Fact]
    public async Task IsValidForRegistration_DetectsValidUser_ReturnsNoError()
    {
        // Normal user (neither bot nor has cheaterRoleId)
        IGuildUser guildUser = Substitute.For<IGuildUser>();
        guildUser.IsBot.Returns(false);
        guildUser.RoleIds.Returns([]);

        _userRepositoryMock.DiscordIdExistsAsync(ExampleDiscordId).Returns(false);
        _userRepositoryMock.LeaderboardIdExistsAsync(1).Returns(false);
        Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser, 1, true);
        Assert.False(isValidForRegistrationResponse.IsFailure);
    }

    [Fact]
    public async Task IsValidForRegistration_DetectsAlreadyRegisteredUser_ReturnsError()
    {
        // Normal user (neither bot nor has cheaterRoleId)
        IGuildUser guildUser = Substitute.For<IGuildUser>();
        guildUser.IsBot.Returns(false);
        guildUser.Id.Returns(0ul);
        guildUser.RoleIds.Returns([]);

        _userRepositoryMock.DiscordIdExistsAsync(ExampleDiscordId).Returns(true);
        Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser, 1, true);
        Assert.True(isValidForRegistrationResponse.IsFailure);
    }

    [Fact]
    public async Task IsValidForRegistration_DetectsAlreadyUsedLeaderboardId_ReturnsError()
    {
        IGuildUser guildUser = Substitute.For<IGuildUser>();
        guildUser.IsBot.Returns(false);
        guildUser.Id.Returns(999ul);
        guildUser.RoleIds.Returns([]);

        _userRepositoryMock.DiscordIdExistsAsync(999ul).Returns(false);
        _userRepositoryMock.LeaderboardIdExistsAsync(1).Returns(true);
        Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser, 1, true);
        Assert.True(isValidForRegistrationResponse.IsFailure);
    }

    [Fact]
    public async Task IsValid_DetectsRegisteredUser_ReturnsNoError()
    {
        // Normal user (neither bot nor has cheaterRoleId)
        IGuildUser guildUser = Substitute.For<IGuildUser>();
        guildUser.IsBot.Returns(false);
        guildUser.Id.Returns(0ul);
        guildUser.RoleIds.Returns([]);

        _userRepositoryMock.DiscordIdExistsAsync(ExampleDiscordId).Returns(true);
        Result isValid = await _sut.IsValid(guildUser, true);
        Assert.False(isValid.IsFailure);
    }

    [Fact]
    public async Task IsValid_DetectsUnregisteredNormalUser_ReturnsError()
    {
        // Normal user (neither bot nor has cheaterRoleId)
        IGuildUser guildUser = Substitute.For<IGuildUser>();
        guildUser.IsBot.Returns(false);
        guildUser.Id.Returns(0ul);
        guildUser.RoleIds.Returns([]);

        _userRepositoryMock.DiscordIdExistsAsync(ExampleDiscordId).Returns(false);
        Result isValid = await _sut.IsValid(guildUser, true);
        Assert.True(isValid.IsFailure);
    }
}
