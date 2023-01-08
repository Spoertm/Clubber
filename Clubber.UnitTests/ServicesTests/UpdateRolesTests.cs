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
	[InlineData(0, new ulong[] { }, _subOneHundredRoleId)]
	[InlineData(100, new ulong[] { }, _oneHundredRoleId)]
	[InlineData(1500, new ulong[] { }, _twelveFiftyRoleId)]
	[InlineData(100, new[] { _oneHundredRoleId, _threeHundredRoleId }, 0)]
	[InlineData(900, new[] { _oneHundredRoleId, _threeHundredRoleId, _twelveThirtyRoleId }, _nineHundredRoleId)]
	public void TestHandleScoreRoles_DetectsRoleInconsistency_ReturnsRolesToBeAdded(
		int scoreInSeconds,
		IReadOnlyCollection<ulong> userRoleIds,
		ulong expectedRoleToAdd)
	{
		(ulong scoreRoleToAdd, _) = _sut.HandleScoreRoles(userRoleIds, scoreInSeconds * 10000);
		Assert.Equal(scoreRoleToAdd, expectedRoleToAdd);
	}

	[Theory]
	[InlineData(0, new ulong[] { }, new ulong[] { })]
	[InlineData(100, new ulong[] { }, new ulong[] { })]
	[InlineData(1500, new ulong[] { }, new ulong[] { })]
	[InlineData(100, new[] { _oneHundredRoleId, _threeHundredRoleId }, new[] { _threeHundredRoleId })]
	[InlineData(900, new[] { _oneHundredRoleId, _threeHundredRoleId, _twelveThirtyRoleId }, new[] { _oneHundredRoleId, _threeHundredRoleId, _twelveThirtyRoleId })]
	public void TestHandleScoreRoles_DetectsRoleInconsistency_ReturnsRolesToBeRemoved(
		int scoreInSeconds,
		IReadOnlyCollection<ulong> userRoleIds,
		ulong[] expectedRolesToRemove)
	{
		(_, ulong[] scoreRolesToRemove) = _sut.HandleScoreRoles(userRoleIds, scoreInSeconds * 10000);
		Assert.Equal(scoreRolesToRemove, expectedRolesToRemove);
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
