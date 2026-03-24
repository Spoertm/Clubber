using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Tests.UnitTests.HelpersTests.TestCaseModels;
using Xunit;

namespace Clubber.Tests.UnitTests.HelpersTests;

public sealed class CollectionUtilsTests
{
    private static readonly ulong _topScoreRoleId = AppConfig.ScoreRoles.MaxBy(s => s.Key).Value;
    private const ulong Sub100RoleId = 461203024128376832;
    private const ulong _100RoleId = 399569183966363648;
    private const ulong _300RoleId = 399569332532674562;
    private const ulong _1230RoleId = 903024433315323915;
    private const ulong _900RoleId = 399570895741386765;
    private const ulong Top1RoleId = 446688666325090310;
    private const ulong Top3RoleId = 472451008342261820;
    private const ulong Top10RoleId = 556255819323277312;
    private const ulong UselessRole1 = 111;
    private const ulong UselessRole2 = 222;

    private static readonly ulong[] _allPossibleRoles =
    [
        _topScoreRoleId,
        Sub100RoleId,
        _100RoleId,
        _300RoleId,
        _1230RoleId,
        _900RoleId,
        Top1RoleId,
        Top3RoleId,
        Top10RoleId,
        UselessRole1,
        UselessRole2,
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
            userRoles: [_100RoleId, _300RoleId, _1230RoleId, UselessRole1],
            allPossibleRoles: _allPossibleRoles,
            rolesToKeep: [_100RoleId],
            expectedRolesToAdd: [],
            expectedRolesToRemove: [_300RoleId, _1230RoleId, UselessRole1]
        ),

        new(
            userRoles: [],
            allPossibleRoles: _allPossibleRoles,
            rolesToKeep: [Top1RoleId],
            expectedRolesToAdd: [Top1RoleId],
            expectedRolesToRemove: []
        ),

        new(
            userRoles: [69, 420, UselessRole1],
            allPossibleRoles: _allPossibleRoles,
            rolesToKeep: [Top1RoleId, _1230RoleId],
            expectedRolesToAdd: [Top1RoleId, _1230RoleId],
            expectedRolesToRemove: [UselessRole1]
        ),

        new(
            userRoles: [69, 420, _1230RoleId, Top10RoleId],
            allPossibleRoles: _allPossibleRoles,
            rolesToKeep: [_1230RoleId, Top10RoleId],
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
