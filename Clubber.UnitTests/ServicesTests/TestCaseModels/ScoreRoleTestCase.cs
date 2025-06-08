using Xunit.Abstractions;

namespace Clubber.UnitTests.ServicesTests.TestCaseModels;

public sealed record ScoreRoleTestCase : IXunitSerializable
{
	public int PlayerTimeInSeconds { get; set; }
	public int ExpectedScoreRoleKey { get; set; }

	// ReSharper disable once UnusedMember.Global
	public ScoreRoleTestCase() { }

	public ScoreRoleTestCase(int playerTimeInSeconds, int expectedScoreRoleKey)
	{
		PlayerTimeInSeconds = playerTimeInSeconds;
		ExpectedScoreRoleKey = expectedScoreRoleKey;
	}

	public void Deserialize(IXunitSerializationInfo info)
	{
		PlayerTimeInSeconds = info.GetValue<int>(nameof(PlayerTimeInSeconds));
		ExpectedScoreRoleKey = info.GetValue<int>(nameof(ExpectedScoreRoleKey));
	}

	public void Serialize(IXunitSerializationInfo info)
	{
		info.AddValue(nameof(PlayerTimeInSeconds), PlayerTimeInSeconds);
		info.AddValue(nameof(ExpectedScoreRoleKey), ExpectedScoreRoleKey);
	}
}
