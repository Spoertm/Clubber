using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.UnitTests.ServicesTests.TestCaseModels;
using Xunit;

namespace Clubber.UnitTests.ServicesTests;

public sealed class ScoreRoleServiceTests
{
	[Theory]
	[MemberData(nameof(GetScoreRoleToKeepTestData))]
	public void GetScoreRoleToKeep_ReturnsExpectedRole(ScoreRoleTestCase testCase)
	{
		(int scoreKey, ulong _) = ScoreRoleService.GetScoreRoleToKeep(testCase.PlayerTimeInSeconds * 10_000);

		Assert.Equal(testCase.ExpectedScoreRoleKey, scoreKey);
	}

	public static TheoryData<ScoreRoleTestCase> GetScoreRoleToKeepTestData =>
	[
		new(playerTimeInSeconds: 0, expectedScoreRoleKey: 0),
		new(playerTimeInSeconds: 1000, expectedScoreRoleKey: 1000),
		new(playerTimeInSeconds: 12, expectedScoreRoleKey: 0),
		new(playerTimeInSeconds: 552, expectedScoreRoleKey: 500),
		new(playerTimeInSeconds: 1500, expectedScoreRoleKey: AppConfig.ScoreRoles.MaxBy(sr => sr.Key).Key)
	];

	[Theory]
	[MemberData(nameof(GetRankRoleToKeepTestData))]
	public void GetRankRoleToKeep_ReturnsExpectedRole(RankRoleTestCase testCase)
	{
		(int rankKey, ulong _) = ScoreRoleService.GetRankRoleToKeep(testCase.PlayerRank);

		Assert.Equal(testCase.ExpectedRankKey, rankKey);
	}

	public static TheoryData<RankRoleTestCase> GetRankRoleToKeepTestData =>
	[
		new(playerRank: 1, expectedRankKey: 1),
		new(playerRank: 2, expectedRankKey: 3),
		new(playerRank: 3, expectedRankKey: 3),
		new(playerRank: 5, expectedRankKey: 10),
		new(playerRank: 10, expectedRankKey: 10),
		new(playerRank: 15, expectedRankKey: 25),
		new(playerRank: 25, expectedRankKey: 25),
		new(playerRank: 30, expectedRankKey: 0)
	];
}
