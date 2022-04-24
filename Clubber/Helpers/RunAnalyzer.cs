using Clubber.Models.DdSplits;
using Clubber.Models.Responses;

namespace Clubber.Helpers;

public static class RunAnalyzer
{
	public static Split[] GetData(DdStatsFullRunResponse ddstatsRun)
	{
		List<Split> splits = new();
		(string Name, int Time) currentSplit = new("0", 0);
		for (int i = 0; i < Split.V3Splits.Length; i++)
		{
			(string Name, int Time) nextSplit = Split.V3Splits[i];
			if (ddstatsRun.GameInfo.GameTime < currentSplit.Time)
				break;

			State? startOfSplitState = Array.Find(ddstatsRun.States, s => (int)s.GameTime == currentSplit.Time);
			State? endOfSplitState = Array.Find(ddstatsRun.States, s => (int)s.GameTime == nextSplit.Time);
			if (startOfSplitState is null || endOfSplitState is null)
				continue;

			int splitValue = endOfSplitState.HomingDaggers - startOfSplitState.HomingDaggers;
			currentSplit = Split.V3Splits[i];
			splits.Add(new(currentSplit.Name, currentSplit.Time, splitValue));
		}

		return splits.ToArray();
	}
}
