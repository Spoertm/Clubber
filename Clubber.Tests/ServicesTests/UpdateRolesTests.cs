using Clubber.Helpers;
using Clubber.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Clubber.Tests.ServicesTests;

public class UpdateRolesTests
{
	private readonly UpdateRolesHelper _sut;
	private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
	private readonly Mock<IWebService> _webserviceMock = new();
	private const ulong SubOneHundredRoleId = 461203024128376832;
	private const ulong OneHundredRoleId = 399569183966363648;
	private const ulong ThreeHundredRoleId = 399569332532674562;
	private const ulong TwelveThirtyRoleId = 903024433315323915;
	private const ulong NineHundredRoleId = 399570895741386765;
	private const ulong Top1RoleId = 446688666325090310;
	private const ulong Top3RoleId = 472451008342261820;
	private const ulong Top10RoleId = 556255819323277312;

	public UpdateRolesTests()
	{
		IConfiguration configMock = new ConfigurationBuilder()
			.AddJsonFile("appsettings.Testing.json")
			.Build();

		_sut = new(configMock, _databaseHelperMock.Object, _webserviceMock.Object);
	}

	[Theory]
	[InlineData(0, new ulong[] { }, SubOneHundredRoleId, new ulong[] { })]
	[InlineData(100 * 10000, new ulong[] { }, OneHundredRoleId, new ulong[] { })]
	[InlineData(1500 * 10000, new ulong[] { }, TwelveThirtyRoleId, new ulong[] { })]
	[InlineData(100 * 10000, new[] { OneHundredRoleId, ThreeHundredRoleId }, 0, new[] { ThreeHundredRoleId })]
	[InlineData(900 * 10000, new[] { OneHundredRoleId, ThreeHundredRoleId, TwelveThirtyRoleId }, NineHundredRoleId, new[] { OneHundredRoleId, ThreeHundredRoleId, TwelveThirtyRoleId })]
	public void HandleScoreRoles_DetectsRoleInconsistency_ReturnsRolesToBeAddedAndRemoved(
		int score,
		IReadOnlyCollection<ulong> userRoleIds,
		ulong expectedRoleToAdd,
		ulong[] expectedRolesToRemove)
	{
		(ulong scoreRoleToAdd, ulong[] scoreRolesToRemove) = _sut.HandleScoreRoles(userRoleIds, score);
		Assert.Equal(scoreRoleToAdd, expectedRoleToAdd);
		Assert.Equal(scoreRolesToRemove, expectedRolesToRemove);
	}

	[Theory]
	[InlineData(1, new ulong[] { }, Top1RoleId, new ulong[] { })]
	[InlineData(2, new ulong[] { }, Top3RoleId, new ulong[] { })]
	[InlineData(3, new ulong[] { }, Top3RoleId, new ulong[] { })]
	[InlineData(10, new ulong[] { }, Top10RoleId, new ulong[] { })]
	[InlineData(50, new ulong[] { }, 0, new ulong[] { })]
	[InlineData(50, new[] { Top1RoleId }, 0, new[] { Top1RoleId })]
	[InlineData(50, new[] { OneHundredRoleId, Top3RoleId }, 0, new[] { Top3RoleId })]
	public void HandleTopRoles_DetectsRoleInconsistency_ReturnsRolesToBeAddedAndRemoved(
		int rank,
		IReadOnlyCollection<ulong> userRoleIds,
		ulong expectedTopRoleToAdd,
		ulong[] expectedTopRolesToRemove)
	{
		(ulong topRoleToAdd, ulong[] topRolesToRemove) = _sut.HandleTopRoles(userRoleIds, rank);
		Assert.Equal(topRoleToAdd, expectedTopRoleToAdd);
		Assert.Equal(topRolesToRemove, expectedTopRolesToRemove);
	}
}
