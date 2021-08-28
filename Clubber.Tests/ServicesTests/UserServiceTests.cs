using Clubber.Helpers;
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
		private readonly Mock<IDatabaseHelper> _databaseHelperMock;

		public UserServiceTests()
		{
			_databaseHelperMock = new();
			_sut = new(_databaseHelperMock.Object);
		}

		[Fact]
		public void IsValidForRegistrationDetectsBot()
		{
			Mock<IGuildUser> guildUser = new();
			guildUser.SetupGet(user => user.IsBot).Returns(true);
			UserValidationResponse response = _sut.IsValidForRegistration(guildUser.Object, true);
			Assert.True(response.IsError);
		}
	}
}
