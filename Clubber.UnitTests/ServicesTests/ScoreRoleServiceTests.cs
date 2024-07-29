using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Xunit;

namespace Clubber.UnitTests.ServicesTests;

public class ScoreRoleServiceTests
{
	[Theory]
	[MemberData(nameof(GetScoreRoleToKeepTestData))]
	public void GetScoreRoleToKeep_ReturnsExpectedRole(int playerTime, int expectedScoreRoleKey)
	{
		(int scoreKey, ulong _) = ScoreRoleService.GetScoreRoleToKeep(playerTime * 10_000);

		Assert.Equal(expectedScoreRoleKey, scoreKey);
	}

	public static IEnumerable<object[]> GetScoreRoleToKeepTestData()
	{
		yield return [0, 0];
		yield return [1000, 1000];
		yield return [12, 0];
		yield return [552, 500];
		yield return [1500, AppConfig.ScoreRoles.MaxBy(sr => sr.Key).Key];
	}

	[Theory]
	[InlineData(1, 1)]
	[InlineData(2, 3)]
	[InlineData(3, 3)]
	[InlineData(5, 10)]
	[InlineData(10, 10)]
	[InlineData(15, 25)]
	[InlineData(25, 25)]
	[InlineData(30, 0)]
	public void GetRankRoleToKeep_ReturnsExpectedRole(int playerRank, int expectedRankKey)
	{
		(int rankKey, ulong _) = ScoreRoleService.GetRankRoleToKeep(playerRank);

		Assert.Equal(expectedRankKey, rankKey);
	}
}
