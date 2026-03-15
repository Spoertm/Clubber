using Clubber.Domain.Helpers;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Xunit;

namespace Clubber.Tests.UnitTests.HelpersTests;

public sealed class RunAnalyzerTests
{
	[Fact]
	public void GetData_RunTooShort_ReturnsEmptyCollection()
	{
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 100f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 0),
				CreateState(gameTime: 100, homingDaggers: 50),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Empty(result);
	}

	[Fact]
	public void GetData_RunEndsBeforeFirstSplit_ReturnsEmptyCollection()
	{
		// First split is at time 366 (named "350")
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 365f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 0),
				CreateState(gameTime: 365, homingDaggers: 100),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Empty(result);
	}

	[Fact]
	public void GetData_RunReachesFirstSplit_ReturnsSingleSplit()
	{
		// First split "350" at time 366
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 366f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 0),
				CreateState(gameTime: 366, homingDaggers: 200),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Single(result);
		Split split = result.First();
		Assert.Equal("350", split.Name);
		Assert.Equal(366, split.Time);
		// 200 - 0 - 105 (special adjustment for 350 split) = 95
		Assert.Equal(95, split.Value);
	}

	[Fact]
	public void GetData_RunReachesMultipleSplits_ReturnsMultipleSplits()
	{
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 800f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 0),
				CreateState(gameTime: 366, homingDaggers: 200),
				CreateState(gameTime: 709, homingDaggers: 400),
				CreateState(gameTime: 800, homingDaggers: 550),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Equal(3, result.Count);

		Split[] splits = result.ToArray();

		// First split: "350" at 366
		Assert.Equal("350", splits[0].Name);
		Assert.Equal(366, splits[0].Time);
		Assert.Equal(95, splits[0].Value); // 200 - 0 - 105

		// Second split: "700" at 709
		Assert.Equal("700", splits[1].Name);
		Assert.Equal(709, splits[1].Time);
		Assert.Equal(200, splits[1].Value); // 400 - 200

		// Third split: "800" at 800
		Assert.Equal("800", splits[2].Name);
		Assert.Equal(800, splits[2].Time);
		Assert.Equal(150, splits[2].Value); // 550 - 400
	}

	[Fact]
	public void GetData_RunReachesAllSplits_ReturnsAllSplits()
	{
		// Last split "1290" at time 1290
		List<State> states = new()
		{
			CreateState(gameTime: 0, homingDaggers: 0),
		};

		int homingDaggers = 0;
		foreach ((string name, int time) in Split.V3Splits)
		{
			homingDaggers += 100;
			states.Add(CreateState(gameTime: time, homingDaggers: homingDaggers));
		}

		DdStatsFullRunResponse run = CreateRun(
			gameTime: 1290f,
			states: states);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Equal(Split.V3Splits.Count, result.Count);
	}

	[Fact]
	public void GetData_MissingStartState_SkipsSplit()
	{
		// Missing state at time 0
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 366f,
			states:
			[
				CreateState(gameTime: 366, homingDaggers: 200),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Empty(result);
	}

	[Fact]
	public void GetData_MissingEndState_SkipsSplitButContinuesWithCorrectStart()
	{
		// Missing state at time 366, but has state at time 0 and 709
		// The "350" split should be skipped, but "700" should be calculated
		// using time 0 as start (since currentSplit updates even when skipped)
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 709f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 0),
				CreateState(gameTime: 709, homingDaggers: 400),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		// Only "700" split is included since "350" is skipped due to missing state at 366
		Assert.Single(result);
		Assert.Equal("700", result.First().Name);
		Assert.Equal(400, result.First().Value); // 400 - 0
	}

	[Fact]
	public void GetData_PartialStates_SkipsMissingSplitsButContinues()
	{
		// Has state at 0 and 366, but missing 709, then has 800
		// "350" is calculated normally, "700" is skipped (missing 709),
		// but "800" is calculated using 366 as start because currentSplit updated
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 800f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 0),
				CreateState(gameTime: 366, homingDaggers: 200),
				CreateState(gameTime: 800, homingDaggers: 550),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		// "350" is calculated, "700" is skipped, "800" uses 366 as start
		Assert.Equal(2, result.Count);

		Split[] splits = result.ToArray();
		Assert.Equal("350", splits[0].Name);
		Assert.Equal(95, splits[0].Value); // 200 - 0 - 105
		Assert.Equal("800", splits[1].Name);
		Assert.Equal(350, splits[1].Value); // 550 - 200
	}

	[Fact]
	public void GetData_ZeroHomingDaggers_ReturnsZeroValue()
	{
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 366f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 50),
				CreateState(gameTime: 366, homingDaggers: 50),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Single(result);
		// 50 - 50 - 105 = -105 (can be negative due to the 350 adjustment)
		Assert.Equal(-105, result.First().Value);
	}

	[Fact]
	public void GetData_NegativeSplitValue_With350Adjustment()
	{
		// The 350 split has a special -105 adjustment that can make values negative
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 366f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 200),
				CreateState(gameTime: 366, homingDaggers: 200),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Single(result);
		Assert.Equal(-105, result.First().Value); // 200 - 200 - 105
	}

	[Fact]
	public void GetData_GameTimeExactlyAtSplitBoundary_IncludesSplit()
	{
		// Game time exactly at 366 should include the 350 split
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 366f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 0),
				CreateState(gameTime: 366, homingDaggers: 150),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Single(result);
		Assert.Equal("350", result.First().Name);
	}

	[Fact]
	public void GetData_StatesWithNonIntegerGameTime_FiltersByIntCast()
	{
		// States have non-integer game times that should be matched by int cast
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 366.7f,
			states:
			[
				CreateState(gameTime: 0.5, homingDaggers: 0),
				CreateState(gameTime: 366.3, homingDaggers: 150),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		// (int)0.5 = 0, (int)366.3 = 366, so states should match
		Assert.Single(result);
		Assert.Equal("350", result.First().Name);
		Assert.Equal(45, result.First().Value); // 150 - 0 - 105
	}

	[Fact]
	public void GetData_Non350Split_NoAdjustment()
	{
		// Second split "700" should not have the -105 adjustment
		DdStatsFullRunResponse run = CreateRun(
			gameTime: 709f,
			states:
			[
				CreateState(gameTime: 0, homingDaggers: 0),
				CreateState(gameTime: 366, homingDaggers: 105),
				CreateState(gameTime: 709, homingDaggers: 305),
			]);

		IReadOnlyCollection<Split> result = RunAnalyzer.GetData(run);

		Assert.Equal(2, result.Count);

		Split[] splits = [.. result];

		// First split: "350" has -105 adjustment
		Assert.Equal("350", splits[0].Name);
		Assert.Equal(0, splits[0].Value); // 105 - 0 - 105

		// Second split: "700" has no adjustment
		Assert.Equal("700", splits[1].Name);
		Assert.Equal(200, splits[1].Value); // 305 - 105
	}

	private static DdStatsFullRunResponse CreateRun(float gameTime, List<State> states)
	{
		return new DdStatsFullRunResponse
		{
			GameInfo = new GameInfo
			{
				GameTime = gameTime,
			},
			States = states,
		};
	}

	private static State CreateState(double gameTime, int homingDaggers)
	{
		return new State
		{
			GameTime = gameTime,
			HomingDaggers = homingDaggers,
		};
	}
}
