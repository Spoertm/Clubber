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
	private const ulong _cheaterRoleId = 666;
	private const ulong _exampleDiscordId = 0;

	public UserServiceTests()
	{
		IConfiguration configMock = new ConfigurationBuilder()
			.AddJsonFile("appsettings.Testing.json")
			.Build();

		AppConfig appConfig = new();
		configMock.GetSection("BotConfig").Bind(appConfig);
		IOptions<AppConfig> options = Options.Create(appConfig);

		_sut = new UserService(options, _userRepositoryMock);
	}

	[Theory]
	[InlineData(true, new ulong[] { 1, 2, 3 })]
	[InlineData(true, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	[InlineData(false, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	public async Task IsValidForRegistration_DetectsBotAndOrCheater_ReturnsError(bool isBot, IReadOnlyCollection<ulong> roleIds)
	{
		IGuildUser guildUser = Substitute.For<IGuildUser>();
		guildUser.IsBot.Returns(isBot);
		guildUser.RoleIds.Returns(roleIds);

		Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser, true);
		Assert.True(isValidForRegistrationResponse.IsFailure);
	}

	[Theory]
	[InlineData(true, new ulong[] { 1, 2, 3 })]
	[InlineData(true, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	[InlineData(false, new ulong[] { 1, 2, 3, _cheaterRoleId })]
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

		_userRepositoryMock.FindAsync(_exampleDiscordId).Returns(default(DdUser));
		Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser, true);
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

		_userRepositoryMock.FindAsync(_exampleDiscordId).Returns(new DdUser(0, 0));
		Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser, true);
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

		_userRepositoryMock.FindAsync(_exampleDiscordId).Returns(new DdUser(0, 0));
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

		_userRepositoryMock.FindAsync(_exampleDiscordId).Returns(default(DdUser));
		Result isValid = await _sut.IsValid(guildUser, true);
		Assert.True(isValid.IsFailure);
	}
}
