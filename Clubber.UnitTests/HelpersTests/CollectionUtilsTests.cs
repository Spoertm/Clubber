using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Xunit;

namespace Clubber.UnitTests.HelpersTests;

public class CollectionUtilsTests
{
	private static readonly ulong _topScoreRoleId = AppConfig.ScoreRoles.MaxBy(s => s.Key).Value;
	private const ulong _sub100RoleId = 461203024128376832;
	private const ulong _100RoleId = 399569183966363648;
	private const ulong _300RoleId = 399569332532674562;
	private const ulong _1230RoleId = 903024433315323915;
	private const ulong _900RoleId = 399570895741386765;
	private const ulong _top1RoleId = 446688666325090310;
	private const ulong _top3RoleId = 472451008342261820;
	private const ulong _top10RoleId = 556255819323277312;
	private const ulong _uselessRole1 = 111;
	private const ulong _uselessRole2 = 222;
	private static readonly IReadOnlyList<ulong> _allPossibleRoles =
	[
		_topScoreRoleId,
		_sub100RoleId,
		_100RoleId,
		_300RoleId,
		_1230RoleId,
		_900RoleId,
		_top1RoleId,
		_top3RoleId,
		_top10RoleId,
		_uselessRole1,
		_uselessRole2,
	];

	[Theory]
	[MemberData(nameof(DetermineRoleChangesTestData))]
	public void DetermineRoleChanges_DetectsRoleInconsistency_ReturnsRolesToBeAddedAndRemoved(
		IReadOnlyCollection<ulong> userRoles,
		IReadOnlyCollection<ulong> allPossibleRoles,
		IReadOnlyCollection<ulong> rolesToKeep,
		ulong[] expectedRolesToBeAdded,
		ulong[] expectedRolesToBeRemoved)
	{
		CollectionChange<ulong> roleUpdate = CollectionUtils.DetermineCollectionChanges(userRoles, [..allPossibleRoles], rolesToKeep);
		Assert.Equal(roleUpdate.ItemsToAdd, expectedRolesToBeAdded);
		Assert.Equal(roleUpdate.ItemsToRemove, expectedRolesToBeRemoved);
	}

	public static IEnumerable<object[]> DetermineRoleChangesTestData()
	{
		yield return
		[
			new[] { _100RoleId, _300RoleId, _1230RoleId, _uselessRole1 },
			_allPossibleRoles,
			new[] { _100RoleId },
			new ulong[] { },
			new[] { _300RoleId, _1230RoleId, _uselessRole1 },
		];

		yield return
		[
			new ulong[] { },
			_allPossibleRoles,
			new[] { _top1RoleId },
			new[] { _top1RoleId },
			new ulong[] { },
		];

		yield return
		[
			new ulong[] { 69, 420, _uselessRole1 },
			_allPossibleRoles,
			new[] { _top1RoleId, _1230RoleId },
			new[] { _top1RoleId, _1230RoleId },
			new[] { _uselessRole1 },
		];

		yield return
		[
			new ulong[] { 69, 420, _1230RoleId, _top10RoleId },
			_allPossibleRoles,
			new[] { _1230RoleId, _top10RoleId },
			new ulong[] { },
			new ulong[] { },
		];
	}

	[Theory]
	[MemberData(nameof(TestData))]
	public void GetSecondsAwayFromNextRoleAndNextRoleId_DetectsSecondsInconsistency_ReturnsSecondsAndRoleId(
		decimal scoreInSeconds,
		decimal? expectedSecondsAwayFromNextRole)
	{
		MilestoneInfo<ulong> milestoneInfo = CollectionUtils.GetNextMileStone((int)(scoreInSeconds * 10_000), AppConfig.ScoreRoles);
		Assert.Equal(expectedSecondsAwayFromNextRole, milestoneInfo.TimeUntilNextMilestone);
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
