using Clubber.Domain.Helpers;
using Clubber.Domain.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Clubber.UnitTests.ServicesTests;

public class UpdateRolesTests
{
	private readonly UpdateRolesHelper _sut;
	private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
	private readonly Mock<IWebService> _webserviceMock = new();
	private const ulong _subOneHundredRoleId = 461203024128376832;
	private const ulong _oneHundredRoleId = 399569183966363648;
	private const ulong _threeHundredRoleId = 399569332532674562;
	private const ulong _twelveThirtyRoleId = 903024433315323915;
	private const ulong _twelveFiftyRoleId = 980126799075876874;
	private const ulong _nineHundredRoleId = 399570895741386765;
	private const ulong _top1RoleId = 446688666325090310;
	private const ulong _top3RoleId = 472451008342261820;
	private const ulong _top10RoleId = 556255819323277312;

	public UpdateRolesTests()
	{
		IConfiguration configMock = new ConfigurationBuilder()
			.AddJsonFile("appsettings.Testing.json")
			.Build();

		_sut = new(configMock, _databaseHelperMock.Object, _webserviceMock.Object);
	}

	[Theory]
	[InlineData(0, new ulong[] { }, _subOneHundredRoleId, new ulong[] { })]
	[InlineData(100 * 10000, new ulong[] { }, _oneHundredRoleId, new ulong[] { })]
	[InlineData(1500 * 10000, new ulong[] { }, _twelveFiftyRoleId, new ulong[] { })]
	[InlineData(100 * 10000, new[] { _oneHundredRoleId, _threeHundredRoleId }, 0, new[] { _threeHundredRoleId })]
	[InlineData(900 * 10000, new[] { _oneHundredRoleId, _threeHundredRoleId, _twelveThirtyRoleId }, _nineHundredRoleId, new[] { _oneHundredRoleId, _threeHundredRoleId, _twelveThirtyRoleId })]
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
	[InlineData(1, new ulong[] { }, _top1RoleId, new ulong[] { })]
	[InlineData(2, new ulong[] { }, _top3RoleId, new ulong[] { })]
	[InlineData(3, new ulong[] { }, _top3RoleId, new ulong[] { })]
	[InlineData(10, new ulong[] { }, _top10RoleId, new ulong[] { })]
	[InlineData(50, new ulong[] { }, 0, new ulong[] { })]
	[InlineData(50, new[] { _top1RoleId, _top3RoleId }, 0, new[] { _top1RoleId, _top3RoleId })]
	[InlineData(50, new[] { _oneHundredRoleId, _top3RoleId }, 0, new[] { _top3RoleId })]
	[InlineData(50, new[] { _oneHundredRoleId, _top3RoleId, _threeHundredRoleId }, 0, new[] { _top3RoleId })]
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

	[Theory]
	[MemberData(nameof(TestData))]
	public void GetSecondsAwayFromNextRoleAndNextRoleId_DetectsSecondsInconsistency_ReturnsSecondsAndRoleId(
		decimal scoreInSeconds,
		decimal? expectedSecondsAwayFromNextRole)
	{
		(decimal? secondsAwayFromNextRole, _) = _sut.GetSecondsAwayFromNextRoleAndNextRoleId((int)(scoreInSeconds * 10000));
		Assert.Equal(expectedSecondsAwayFromNextRole, secondsAwayFromNextRole);
	}

	public static IEnumerable<object?[]> TestData => new List<object?[]>
	{
		new object?[] { 0M, 100M },
		new object?[] { 35M, 65M },
		new object?[] { 3000M, null },
		new object?[] { 99.1238M, 0.8762M },
		new object?[] { 700.5000M, 99.5M },
		new object?[] { 532M, 68M },
		new object?[] { 900M, 50M },
	};
}
