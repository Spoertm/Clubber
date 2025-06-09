using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.UnitTests.HelpersTests.TestCaseModels;
using Xunit;

namespace Clubber.UnitTests.HelpersTests;

public sealed class CollectionUtilsTests
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

	private static readonly ulong[] _allPossibleRoles =
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
	public void DetermineRoleChanges_DetectsRoleInconsistency_ReturnsRolesToBeAddedAndRemoved(RoleChangeTestCase testCase)
	{
		CollectionChange<ulong> roleUpdate = CollectionUtils.DetermineCollectionChanges(
			testCase.UserRoles,
			testCase.AllPossibleRoles,
			testCase.RolesToKeep);

		Assert.Equal(testCase.ExpectedRolesToAdd, roleUpdate.ItemsToAdd);
		Assert.Equal(testCase.ExpectedRolesToRemove, roleUpdate.ItemsToRemove);
	}

	public static TheoryData<RoleChangeTestCase> DetermineRoleChangesTestData =>
	[
		new(
			userRoles: [_100RoleId, _300RoleId, _1230RoleId, _uselessRole1],
			allPossibleRoles: _allPossibleRoles,
			rolesToKeep: [_100RoleId],
			expectedRolesToAdd: [],
			expectedRolesToRemove: [_300RoleId, _1230RoleId, _uselessRole1]
		),

		new(
			userRoles: [],
			allPossibleRoles: _allPossibleRoles,
			rolesToKeep: [_top1RoleId],
			expectedRolesToAdd: [_top1RoleId],
			expectedRolesToRemove: []
		),

		new(
			userRoles: [69, 420, _uselessRole1],
			allPossibleRoles: _allPossibleRoles,
			rolesToKeep: [_top1RoleId, _1230RoleId],
			expectedRolesToAdd: [_top1RoleId, _1230RoleId],
			expectedRolesToRemove: [_uselessRole1]
		),

		new(
			userRoles: [69, 420, _1230RoleId, _top10RoleId],
			allPossibleRoles: _allPossibleRoles,
			rolesToKeep: [_1230RoleId, _top10RoleId],
			expectedRolesToAdd: [],
			expectedRolesToRemove: []
		)
	];

	[Theory]
	[MemberData(nameof(TestData))]
	public void GetSecondsAwayFromNextRoleAndNextRoleId_DetectsSecondsInconsistency_ReturnsSecondsAndRoleId(MilestoneTestCase testCase)
	{
		MilestoneInfo<ulong> milestoneInfo = CollectionUtils.GetNextMileStone(
			(int)(testCase.ScoreInSeconds * 10_000),
			AppConfig.ScoreRoles);

		Assert.Equal(testCase.ExpectedSecondsAwayFromNextRole, milestoneInfo.TimeUntilNextMilestone);
	}

	public static TheoryData<MilestoneTestCase> TestData =>
	[
		new(scoreInSeconds: 0M, expectedSecondsAwayFromNextRole: 100M),
		new(scoreInSeconds: 35M, expectedSecondsAwayFromNextRole: 65M),
		new(scoreInSeconds: 3000M, expectedSecondsAwayFromNextRole: 0M),
		new(scoreInSeconds: 99.1238M, expectedSecondsAwayFromNextRole: 0.8762M),
		new(scoreInSeconds: 700.5000M, expectedSecondsAwayFromNextRole: 99.5M),
		new(scoreInSeconds: 532M, expectedSecondsAwayFromNextRole: 68M),
		new(scoreInSeconds: 900M, expectedSecondsAwayFromNextRole: 50M)
	];
}
