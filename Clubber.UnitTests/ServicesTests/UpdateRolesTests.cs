using Clubber.Discord.Helpers;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Clubber.UnitTests.ServicesTests;

public class UpdateRolesTests
{
	private readonly UpdateRolesHelper _sut;
	private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
	private readonly Mock<IWebService> _webserviceMock = new();
	private static readonly ulong _topScoreRoleId = UpdateRolesHelper.ScoreRoles.MaxBy(s => s.Key).Value;
	private const ulong _sub100RoleId = 461203024128376832;
	private const ulong _100RoleId = 399569183966363648;
	private const ulong _300RoleId = 399569332532674562;
	private const ulong _1230RoleId = 903024433315323915;
	private const ulong _900RoleId = 399570895741386765;
	private const ulong _top1RoleId = 446688666325090310;
	private const ulong _top3RoleId = 472451008342261820;
	private const ulong _top10RoleId = 556255819323277312;

	public UpdateRolesTests()
	{
		IConfiguration configMock = new ConfigurationBuilder()
			.AddJsonFile("appsettings.Testing.json")
			.Build();

		AppConfig appConfig = new();
		configMock.Bind(appConfig);
		IOptions<AppConfig> options = Options.Create(appConfig);

		_sut = new(options, _databaseHelperMock.Object, _webserviceMock.Object);
	}

	[Theory]
	[InlineData(0, new ulong[] { }, _sub100RoleId)]
	[InlineData(100, new ulong[] { }, _100RoleId)]
	[InlineData(100, new[] { _100RoleId, _300RoleId }, 0)]
	[InlineData(900, new[] { _100RoleId, _300RoleId, _1230RoleId }, _900RoleId)]
	public void TestHandleScoreRoles_DetectsRoleInconsistency_ReturnsRolesToBeAdded(
		int scoreInSeconds,
		IReadOnlyCollection<ulong> userRoleIds,
		ulong expectedRoleToAdd)
	{
		(ulong scoreRoleToAdd, _) = _sut.HandleScoreRoles(userRoleIds, scoreInSeconds * 10_000);
		Assert.Equal(scoreRoleToAdd, expectedRoleToAdd);
	}

	[Fact]
	public void TestHandleScoreRolesTopScore_DetectsRoleInconsistency_ReturnsRolesToBeAdded()
	{
		const int scoreInSeconds = 2000;
		IReadOnlyCollection<ulong> userRoleIds = [];
		ulong expectedRoleToAdd = _topScoreRoleId;

		(ulong scoreRoleToAdd, _) = _sut.HandleScoreRoles(userRoleIds, scoreInSeconds * 10_000);
		Assert.Equal(scoreRoleToAdd, expectedRoleToAdd);
	}

	[Theory]
	[InlineData(0, new ulong[] { }, new ulong[] { })]
	[InlineData(100, new ulong[] { }, new ulong[] { })]
	[InlineData(1500, new ulong[] { }, new ulong[] { })]
	[InlineData(100, new[] { _100RoleId, _300RoleId }, new[] { _300RoleId })]
	[InlineData(900, new[] { _100RoleId, _300RoleId, _1230RoleId }, new[] { _100RoleId, _300RoleId, _1230RoleId })]
	public void TestHandleScoreRoles_DetectsRoleInconsistency_ReturnsRolesToBeRemoved(
		int scoreInSeconds,
		IReadOnlyCollection<ulong> userRoleIds,
		ulong[] expectedRolesToRemove)
	{
		(_, ulong[] scoreRolesToRemove) = _sut.HandleScoreRoles(userRoleIds, scoreInSeconds * 10_000);
		Assert.Equal(scoreRolesToRemove, expectedRolesToRemove);
	}

	[Theory]
	[InlineData(1, new ulong[] { }, _top1RoleId)]
	[InlineData(2, new ulong[] { }, _top3RoleId)]
	[InlineData(3, new ulong[] { }, _top3RoleId)]
	[InlineData(10, new ulong[] { }, _top10RoleId)]
	[InlineData(50, new ulong[] { }, 0)]
	[InlineData(50, new[] { _top1RoleId, _top3RoleId }, 0)]
	[InlineData(50, new[] { _100RoleId, _top3RoleId }, 0)]
	[InlineData(50, new[] { _100RoleId, _top3RoleId, _300RoleId }, 0)]
	public void HandleTopRoles_DetectsRoleInconsistency_ReturnsRolesToBeAdded(
		int rank,
		IReadOnlyCollection<ulong> userRoleIds,
		ulong expectedTopRoleToAdd)
	{
		(ulong topRoleToAdd, _) = _sut.HandleTopRoles(userRoleIds, rank);
		Assert.Equal(topRoleToAdd, expectedTopRoleToAdd);
	}

	[Theory]
	[InlineData(1, new ulong[] { }, new ulong[] { })]
	[InlineData(2, new ulong[] { }, new ulong[] { })]
	[InlineData(3, new ulong[] { }, new ulong[] { })]
	[InlineData(10, new ulong[] { }, new ulong[] { })]
	[InlineData(50, new ulong[] { }, new ulong[] { })]
	[InlineData(50, new[] { _top1RoleId, _top3RoleId }, new[] { _top1RoleId, _top3RoleId })]
	[InlineData(50, new[] { _100RoleId, _top3RoleId }, new[] { _top3RoleId })]
	[InlineData(50, new[] { _100RoleId, _top3RoleId, _300RoleId }, new[] { _top3RoleId })]
	public void HandleTopRoles_DetectsRoleInconsistency_ReturnsRolesToBeRemoved(
		int rank,
		IReadOnlyCollection<ulong> userRoleIds,
		ulong[] expectedTopRolesToRemove)
	{
		(_, ulong[] topRolesToRemove) = _sut.HandleTopRoles(userRoleIds, rank);
		Assert.Equal(topRolesToRemove, expectedTopRolesToRemove);
	}

	[Theory]
	[MemberData(nameof(TestData))]
	public void GetSecondsAwayFromNextRoleAndNextRoleId_DetectsSecondsInconsistency_ReturnsSecondsAndRoleId(
		decimal scoreInSeconds,
		decimal? expectedSecondsAwayFromNextRole)
	{
		(decimal? secondsAwayFromNextRole, _) = _sut.GetSecondsAwayFromNextRoleAndNextRoleId((int)(scoreInSeconds * 10_000));
		Assert.Equal(expectedSecondsAwayFromNextRole, secondsAwayFromNextRole);
	}

	public static IEnumerable<object?[]> TestData => new List<object?[]>
	{
		new object?[] { 0M, 100M },
		new object?[] { 35M, 65M },
		new object?[] { 3000M, 0M },
		new object?[] { 99.1238M, 0.8762M },
		new object?[] { 700.5000M, 99.5M },
		new object?[] { 532M, 68M },
		new object?[] { 900M, 50M },
	};
}
