using Xunit.Abstractions;

namespace Clubber.UnitTests.ServicesTests.TestCaseModels;

public sealed record RankRoleTestCase : IXunitSerializable
{
	public int PlayerRank { get; set; }
	public int ExpectedRankKey { get; set; }

	// ReSharper disable once UnusedMember.Global
	public RankRoleTestCase() { }

	public RankRoleTestCase(int playerRank, int expectedRankKey)
	{
		PlayerRank = playerRank;
		ExpectedRankKey = expectedRankKey;
	}

	public void Deserialize(IXunitSerializationInfo info)
	{
		PlayerRank = info.GetValue<int>(nameof(PlayerRank));
		ExpectedRankKey = info.GetValue<int>(nameof(ExpectedRankKey));
	}

	public void Serialize(IXunitSerializationInfo info)
	{
		info.AddValue(nameof(PlayerRank), PlayerRank);
		info.AddValue(nameof(ExpectedRankKey), ExpectedRankKey);
	}
}
