using System.Collections.Generic;
using Clubber.Configuration;
using Clubber.Helpers;
using Clubber.Models;
using Clubber.Models.Responses;
using Clubber.Services;
using Discord;
using Moq;
using Xunit;

namespace Clubber.Tests.ServicesTests
{
	public class UserServiceTests
	{
		private readonly UserService _sut;
		private readonly Mock<IConfig> _configMock;
		private readonly Mock<IDatabaseHelper> _databaseHelperMock;
		private const ulong CheaterRoleId = 666;

		public UserServiceTests()
		{
			_configMock = new();
			_databaseHelperMock = new();
			_sut = new(_configMock.Object, _databaseHelperMock.Object);
		}

		[Theory]
		[InlineData(true, new ulong[] { 1, 2, 3 })]
		[InlineData(true, new ulong[] { 1, 2, 3, CheaterRoleId })]
		[InlineData(false, new ulong[] { 1, 2, 3, CheaterRoleId })]
		public void IsValidForRegistrationDetectsTypeOfUser(bool isBot, IReadOnlyCollection<ulong> roleIds)
		{
			Mock<IGuildUser> guildUser = new();
			guildUser.SetupGet(user => user.IsBot).Returns(isBot);
			guildUser.SetupGet(user => user.RoleIds).Returns(roleIds);
			_configMock.SetupGet(cm => cm.CheaterRoleId).Returns(CheaterRoleId);
			UserValidationResponse isValidForRegistrationResponse = _sut.IsValidForRegistration(guildUser.Object, true);
			Assert.True(isValidForRegistrationResponse.IsError);
		}

		[Theory]
		[InlineData(true, new ulong[] { 1, 2, 3 })]
		[InlineData(true, new ulong[] { 1, 2, 3, CheaterRoleId })]
		[InlineData(false, new ulong[] { 1, 2, 3, CheaterRoleId })]
		public void IsValidDetectsTypeOfUser(bool isBot, IReadOnlyCollection<ulong> roleIds)
		{
			Mock<IGuildUser> guildUser = new();
			guildUser.SetupGet(user => user.IsBot).Returns(isBot);
			guildUser.SetupGet(user => user.RoleIds).Returns(roleIds);
			_configMock.SetupGet(cm => cm.CheaterRoleId).Returns(CheaterRoleId);
			UserValidationResponse isValidResponse = _sut.IsValid(guildUser.Object, true);
			Assert.True(isValidResponse.IsError);
		}

		[Fact]
		public void IsValidForRegistrationDetectsValidUser()
		{
			// Normal user (neither bot nor has cheaterRoleId)
			Mock<IGuildUser> guildUser = new();
			guildUser.SetupGet(user => user.IsBot).Returns(false);
			guildUser.SetupGet(user => user.RoleIds).Returns(new ulong[] { 1, 2, 3 });

			_databaseHelperMock.Setup(dbhm => dbhm.GetDdUserByDiscordId(123)).Returns(default(DdUser));
			UserValidationResponse isValidForRegistrationResponse = _sut.IsValidForRegistration(guildUser.Object, true);
			Assert.False(isValidForRegistrationResponse.IsError);
		}

		[Fact]
		public void IsValidForRegistrationDetectsAlreadyRegisteredUser()
		{
			// Normal user (neither bot nor has cheaterRoleId)
			Mock<IGuildUser> guildUser = new();
			guildUser.SetupGet(user => user.IsBot).Returns(false);
			guildUser.SetupGet(user => user.Id).Returns(123);
			guildUser.SetupGet(user => user.RoleIds).Returns(new ulong[] { 1, 2, 3 });

			_databaseHelperMock.Setup(dbhm => dbhm.GetDdUserByDiscordId(123)).Returns(new DdUser(123, 123));
			UserValidationResponse isValidForRegistrationResponse = _sut.IsValidForRegistration(guildUser.Object, true);
			Assert.True(isValidForRegistrationResponse.IsError);
		}

		[Fact]
		public void IsValidDetectsRegisteredUser()
		{
			// Normal user (neither bot nor has cheaterRoleId)
			Mock<IGuildUser> guildUser = new();
			guildUser.SetupGet(user => user.IsBot).Returns(false);
			guildUser.SetupGet(user => user.Id).Returns(123);
			guildUser.SetupGet(user => user.RoleIds).Returns(new ulong[] { 1, 2, 3 });

			_databaseHelperMock.Setup(dbhm => dbhm.GetDdUserByDiscordId(123)).Returns(new DdUser(123, 123));
			UserValidationResponse isValid = _sut.IsValid(guildUser.Object, true);
			Assert.False(isValid.IsError);
		}

		[Fact]
		public void IsValidDetectsUnregisteredNormalUser()
		{
			// Normal user (neither bot nor has cheaterRoleId)
			Mock<IGuildUser> guildUser = new();
			guildUser.SetupGet(user => user.IsBot).Returns(false);
			guildUser.SetupGet(user => user.Id).Returns(123);
			guildUser.SetupGet(user => user.RoleIds).Returns(new ulong[] { 1, 2, 3 });

			_databaseHelperMock.Setup(dbhm => dbhm.GetDdUserByDiscordId(123)).Returns(default(DdUser));
			UserValidationResponse isValid = _sut.IsValid(guildUser.Object, true);
			Assert.True(isValid.IsError);
		}
	}
}
