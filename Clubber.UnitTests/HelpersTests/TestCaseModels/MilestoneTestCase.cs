using Xunit.Abstractions;

namespace Clubber.UnitTests.HelpersTests.TestCaseModels;

public sealed record MilestoneTestCase : IXunitSerializable
{
	public decimal ScoreInSeconds { get; set; }
	public decimal? ExpectedSecondsAwayFromNextRole { get; set; }

	// ReSharper disable once UnusedMember.Global
	public MilestoneTestCase() { }

	public MilestoneTestCase(decimal scoreInSeconds, decimal? expectedSecondsAwayFromNextRole)
	{
		ScoreInSeconds = scoreInSeconds;
		ExpectedSecondsAwayFromNextRole = expectedSecondsAwayFromNextRole;
	}

	public void Deserialize(IXunitSerializationInfo info)
	{
		ScoreInSeconds = info.GetValue<decimal>(nameof(ScoreInSeconds));
		ExpectedSecondsAwayFromNextRole = info.GetValue<decimal?>(nameof(ExpectedSecondsAwayFromNextRole));
	}

	public void Serialize(IXunitSerializationInfo info)
	{
		info.AddValue(nameof(ScoreInSeconds), ScoreInSeconds);
		info.AddValue(nameof(ExpectedSecondsAwayFromNextRole), ExpectedSecondsAwayFromNextRole);
	}
}
