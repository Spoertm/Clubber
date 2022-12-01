using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Clubber.UnitTests.ServicesTests;

public class UserServiceTests
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

		_sut = new(configMock, _databaseHelperMock.Object);
	}

	[Theory]
	[InlineData(true, new ulong[] { 1, 2, 3 })]
	[InlineData(true, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	[InlineData(false, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	public void IsValidForRegistration_DetectsBotAndOrCheater_ReturnsError(bool isBot, IReadOnlyCollection<ulong> roleIds)
	{
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(isBot);
		guildUser.SetupGet(user => user.RoleIds).Returns(roleIds);
		UserValidationResponse isValidForRegistrationResponse = _sut.IsValidForRegistration(guildUser.Object, true);
		Assert.True(isValidForRegistrationResponse.IsError);
	}

	[Theory]
	[InlineData(true, new ulong[] { 1, 2, 3 })]
	[InlineData(true, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	[InlineData(false, new ulong[] { 1, 2, 3, _cheaterRoleId })]
	public void IsValid_DetectsBotAndOrCheater_ReturnsError(bool isBot, IReadOnlyCollection<ulong> roleIds)
	{
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(isBot);
		guildUser.SetupGet(user => user.RoleIds).Returns(roleIds);
		UserValidationResponse isValidResponse = _sut.IsValid(guildUser.Object, true);
		Assert.True(isValidResponse.IsError);
	}

	[Fact]
	public void IsValidForRegistration_DetectsValidUser_ReturnsNoError()
	{
		// Normal user (neither bot nor has cheaterRoleId)
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(false);
		guildUser.SetupGet(user => user.RoleIds).Returns(Array.Empty<ulong>());

		_databaseHelperMock.Setup(dbhm => dbhm.GetDdUserBy(_exampleDiscordId)).Returns(default(DdUser));
		UserValidationResponse isValidForRegistrationResponse = _sut.IsValidForRegistration(guildUser.Object, true);
		Assert.False(isValidForRegistrationResponse.IsError);
	}

	[Fact]
	public void IsValidForRegistration_DetectsAlreadyRegisteredUser_ReturnsError()
	{
		// Normal user (neither bot nor has cheaterRoleId)
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(false);
		guildUser.SetupGet(user => user.Id).Returns(0);
		guildUser.SetupGet(user => user.RoleIds).Returns(Array.Empty<ulong>());

		_databaseHelperMock.Setup(dbhm => dbhm.GetDdUserBy(_exampleDiscordId)).Returns(new DdUser(0, 0));
		UserValidationResponse isValidForRegistrationResponse = _sut.IsValidForRegistration(guildUser.Object, true);
		Assert.True(isValidForRegistrationResponse.IsError);
	}

	[Fact]
	public void IsValid_DetectsRegisteredUser_ReturnsNoError()
	{
		// Normal user (neither bot nor has cheaterRoleId)
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(false);
		guildUser.SetupGet(user => user.Id).Returns(0);
		guildUser.SetupGet(user => user.RoleIds).Returns(Array.Empty<ulong>());

		_databaseHelperMock.Setup(dbhm => dbhm.GetDdUserBy(_exampleDiscordId)).Returns(new DdUser(0, 0));
		UserValidationResponse isValid = _sut.IsValid(guildUser.Object, true);
		Assert.False(isValid.IsError);
	}

	[Fact]
	public void IsValid_DetectsUnregisteredNormalUser_ReturnsError()
	{
		// Normal user (neither bot nor has cheaterRoleId)
		Mock<IGuildUser> guildUser = new();
		guildUser.SetupGet(user => user.IsBot).Returns(false);
		guildUser.SetupGet(user => user.Id).Returns(0);
		guildUser.SetupGet(user => user.RoleIds).Returns(Array.Empty<ulong>());

		_databaseHelperMock.Setup(dbhm => dbhm.GetDdUserBy(_exampleDiscordId)).Returns(default(DdUser));
		UserValidationResponse isValid = _sut.IsValid(guildUser.Object, true);
		Assert.True(isValid.IsError);
	}
}
