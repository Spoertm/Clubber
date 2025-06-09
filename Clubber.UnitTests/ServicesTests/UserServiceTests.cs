using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Clubber.UnitTests.ServicesTests;

public sealed class UserServiceTests
{
	private readonly UserService _sut;
	private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
	private const ulong _cheaterRoleId = 666;
	private const ulong _exampleDiscordId = 0;

	public UserServiceTests()
	{
		IConfiguration configMock = new ConfigurationBuilder()
			.AddJsonFile("appsettings.Testing.json")
			.Build();

		AppConfig appConfig = new();
		configMock.Bind(appConfig);
		IOptions<AppConfig> options = Options.Create(appConfig);

		_sut = new UserService(options, _databaseHelperMock.Object);
	}

	[Theory]
	[InlineData(true, new ulong[] { 1, 2, 3 })]
	[InlineData(true, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	[InlineData(false, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	public async Task IsValidForRegistration_DetectsBotAndOrCheater_ReturnsError(bool isBot, IReadOnlyCollection<ulong> roleIds)
	{
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(isBot);
		guildUser.SetupGet(user => user.RoleIds).Returns(roleIds);

		Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser.Object, true);
		Assert.True(isValidForRegistrationResponse.IsFailure);
	}

	[Theory]
	[InlineData(true, new ulong[] { 1, 2, 3 })]
	[InlineData(true, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	[InlineData(false, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	public async Task IsValid_DetectsBotAndOrCheater_ReturnsError(bool isBot, IReadOnlyCollection<ulong> roleIds)
	{
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(isBot);
		guildUser.SetupGet(user => user.RoleIds).Returns(roleIds);

		Result isValidResponse = await _sut.IsValid(guildUser.Object, true);
		Assert.True(isValidResponse.IsFailure);
	}

	[Fact]
	public async Task IsValidForRegistration_DetectsValidUser_ReturnsNoError()
	{
		// Normal user (neither bot nor has cheaterRoleId)
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(false);
		guildUser.SetupGet(user => user.RoleIds).Returns([]);

		_databaseHelperMock.Setup(dbhm => dbhm.FindRegisteredUser(_exampleDiscordId).Result).Returns(default(DdUser));
		Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser.Object, true);
		Assert.False(isValidForRegistrationResponse.IsFailure);
	}

	[Fact]
	public async Task IsValidForRegistration_DetectsAlreadyRegisteredUser_ReturnsError()
	{
		// Normal user (neither bot nor has cheaterRoleId)
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(false);
		guildUser.SetupGet(user => user.Id).Returns(0);
		guildUser.SetupGet(user => user.RoleIds).Returns([]);

		_databaseHelperMock.Setup(dbhm => dbhm.FindRegisteredUser(_exampleDiscordId).Result).Returns(new DdUser(0, 0));
		Result isValidForRegistrationResponse = await _sut.IsValidForRegistration(guildUser.Object, true);
		Assert.True(isValidForRegistrationResponse.IsFailure);
	}

	[Fact]
	public async Task IsValid_DetectsRegisteredUser_ReturnsNoError()
	{
		// Normal user (neither bot nor has cheaterRoleId)
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(false);
		guildUser.SetupGet(user => user.Id).Returns(0);
		guildUser.SetupGet(user => user.RoleIds).Returns([]);

		_databaseHelperMock.Setup(dbhm => dbhm.FindRegisteredUser(_exampleDiscordId).Result).Returns(new DdUser(0, 0));
		Result isValid = await _sut.IsValid(guildUser.Object, true);
		Assert.False(isValid.IsFailure);
	}

	[Fact]
	public async Task IsValid_DetectsUnregisteredNormalUser_ReturnsError()
	{
		// Normal user (neither bot nor has cheaterRoleId)
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(false);
		guildUser.SetupGet(user => user.Id).Returns(0);
		guildUser.SetupGet(user => user.RoleIds).Returns([]);

		_databaseHelperMock.Setup(dbhm => dbhm.FindRegisteredUser(_exampleDiscordId).Result).Returns(default(DdUser));
		Result isValid = await _sut.IsValid(guildUser.Object, true);
		Assert.True(isValid.IsFailure);
	}
}
