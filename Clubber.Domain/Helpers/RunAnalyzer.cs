﻿using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Helpers;

public static class RunAnalyzer
{
	public static Split[] GetData(DdStatsFullRunResponse ddstatsRun)
	{
		List<Split> splits = [];
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
			if (nextSplit.Name == "350")
				splitValue -= 105;

			currentSplit = Split.V3Splits[i];
			splits.Add(new(currentSplit.Name, currentSplit.Time, splitValue));
		}

		return splits.ToArray();
	}
}
