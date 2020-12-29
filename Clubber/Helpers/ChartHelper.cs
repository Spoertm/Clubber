using Clubber.Files;
using MongoDB.Driver;
using QuickChart;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class ChartHelper
	{
		public async Task<Stream> GetEveryXBracketChartStream(int bracketSize, int bottomLimit, int topLimit, string property)
		{
			LeaderboardData LB = new LeaderboardData();

			if (LB.PlayerList.Count == 0)
				return null;

			if (property.Equals(PropertyType.Seconds, System.StringComparison.InvariantCultureIgnoreCase))
			{
				return await GetSecondsChartStream(LB.PlayerList, bracketSize, bottomLimit, topLimit);
			}
			else if (property.Equals(PropertyType.Kills, System.StringComparison.InvariantCultureIgnoreCase))
			{
				return await GetKillsChartStream(LB.PlayerList, bracketSize, bottomLimit, topLimit);
			}
			else if (property.Equals(PropertyType.Gems, System.StringComparison.InvariantCultureIgnoreCase))
			{
				return await GetGemsChartStream(LB.PlayerList, bracketSize, bottomLimit, topLimit);
			}
			else if (property.Equals(PropertyType.DaggersHit, System.StringComparison.InvariantCultureIgnoreCase))
			{
				return await GetDaggersHitChartStream(LB.PlayerList, bracketSize, bottomLimit, topLimit);
			}
			else if (property.Equals(PropertyType.DaggersFired, System.StringComparison.InvariantCultureIgnoreCase))
			{
				return await GetDaggersFiredChartStream(LB.PlayerList, bracketSize, bottomLimit, topLimit);
			}
			else
				return await GetHitAccuracyChartStream(LB.PlayerList, bracketSize, bottomLimit, topLimit);
		}

		private static Task<Stream> GetSecondsChartStream(List<LbDataPlayer> playerList, int bracketSize, int bottomLimit, int topLimit)
		{
			List<LbDataPlayer> times = playerList.GetRange(topLimit - 1, bottomLimit - topLimit + 1);

			float maxScore = times[0].Time;
			float minScore = times.Last().Time;
			times.Reverse();

			List<int> yValues = new List<int>();
			int upper;

			if (minScore == maxScore)
			{
				yValues.Add(times.Count);
			}
			else
			{
				for (float i = minScore; i <= maxScore; i = upper)
				{
					upper = MoveUp((int)i, bracketSize);
					yValues.Add(times.Count(p => p.Time >= i && p.Time < upper));
				}
			}

			string labels = GetEveryXLabelFormatted((int)minScore, (int)maxScore, bracketSize);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Number of players", $"Score frequency - rank {topLimit}-{bottomLimit}");

			return GetStreamFromLink(qc.GetShortUrl());
		}

		private static Task<Stream> GetKillsChartStream(List<LbDataPlayer> playerList, int bracketSize, int bottomLimit, int topLimit)
		{
			playerList = playerList.GetRange(topLimit - 1, bottomLimit - topLimit + 1);

			float maxScore = playerList[0].Time;
			float minScore = playerList.Last().Time;
			playerList.Reverse();

			List<double> yValues = new List<double>();
			int upperScore;

			if (minScore == maxScore)
			{
				yValues.Add(playerList.Average(p => p.Kills));
			}
			else
			{
				for (float i = minScore; i <= maxScore; i = upperScore)
				{
					upperScore = MoveUp((int)i, bracketSize);
					IEnumerable<LbDataPlayer> toBeAveraged = playerList.Where(p => p.Time >= i && p.Time < upperScore);
					if (!toBeAveraged.Any())
						yValues.Add(0);
					else
						yValues.Add(toBeAveraged.Average(p => p.Kills));
				}
			}

			string labels = GetEveryXLabelFormatted((int)minScore, (int)maxScore, bracketSize);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Avg. number of kills", $"Average kills per PB bracket - rank {topLimit}-{bottomLimit}", true);

			return GetStreamFromLink(qc.GetShortUrl());
		}

		private static Task<Stream> GetGemsChartStream(List<LbDataPlayer> playerList, int bracketSize, int bottomLimit, int topLimit)
		{
			playerList = playerList.GetRange(topLimit - 1, bottomLimit - topLimit + 1);

			float maxScore = playerList[0].Time;
			float minScore = playerList.Last().Time;
			playerList.Reverse();

			List<double> yValues = new List<double>();
			int upperScore;

			if (minScore == maxScore)
			{
				yValues.Add(playerList.Average(p => p.Gems));
			}
			else
			{
				for (float i = minScore; i <= maxScore; i = upperScore)
				{
					upperScore = MoveUp((int)i, bracketSize);
					var toBeAveraged = playerList.Where(p => p.Time >= i && p.Time < upperScore);
					if (!toBeAveraged.Any())
						yValues.Add(0);
					else
						yValues.Add(toBeAveraged.Average(p => p.Gems));
				}
			}

			string labels = GetEveryXLabelFormatted((int)minScore, (int)maxScore, bracketSize);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Avg. number of gems", $"Average gems per PB bracket - rank {topLimit}-{bottomLimit}", true);

			return GetStreamFromLink(qc.GetShortUrl());
		}

		private static Task<Stream> GetDaggersHitChartStream(List<LbDataPlayer> playerList, int bracketSize, int bottomLimit, int topLimit)
		{
			playerList = playerList.GetRange(topLimit - 1, bottomLimit - topLimit + 1);

			float maxScore = playerList[0].Time;
			float minScore = playerList.Last().Time;
			playerList.Reverse();

			List<double> yValues = new List<double>();
			int upperScore, startIndex = 0;

			if (minScore == maxScore)
			{
				yValues.Add(playerList.Average(p => p.DaggersHit));
			}
			else
			{
				for (float i = minScore; i <= maxScore; i = upperScore)
				{
					upperScore = MoveUp((int)i, bracketSize);
					var toBeAveraged = playerList.Skip(startIndex).Where(p => p.Time >= i && p.Time < upperScore);
					if (!toBeAveraged.Any())
						yValues.Add(0);
					else
						yValues.Add(toBeAveraged.Average(p => p.DaggersHit));
				}
			}

			string labels = GetEveryXLabelFormatted((int)minScore, (int)maxScore, bracketSize);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Avg. number of daggers hit", $"Average daggers hit per PB bracket - rank {topLimit}-{bottomLimit}", true);

			return GetStreamFromLink(qc.GetShortUrl());
		}

		private static Task<Stream> GetDaggersFiredChartStream(List<LbDataPlayer> playerList, int bracketSize, int bottomLimit, int topLimit)
		{
			playerList = playerList.GetRange(topLimit - 1, bottomLimit - topLimit + 1);

			float maxScore = playerList[0].Time;
			float minScore = playerList.Last().Time;
			playerList.Reverse();

			List<double> yValues = new List<double>();
			int upperScore, startIndex = 0;

			if (minScore == maxScore)
			{
				yValues.Add(playerList.Average(p => p.DaggersFired));
			}
			else
			{
				for (float i = minScore; i <= maxScore; i = upperScore)
				{
					upperScore = MoveUp((int)i, bracketSize);
					var toBeAveraged = playerList.Skip(startIndex).Where(p => p.Time >= i && p.Time < upperScore);
					if (!toBeAveraged.Any())
						yValues.Add(0);
					else
						yValues.Add(toBeAveraged.Average(p => p.DaggersFired));
				}
			}

			string labels = GetEveryXLabelFormatted((int)minScore, (int)maxScore, bracketSize);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Avg. number of daggers fired", $"Average daggers fired per PB bracket - rank {topLimit}-{bottomLimit}", true);

			return GetStreamFromLink(qc.GetShortUrl());
		}

		private static Task<Stream> GetHitAccuracyChartStream(List<LbDataPlayer> playerList, int bracketSize, int bottomLimit, int topLimit)
		{
			playerList = playerList.GetRange(topLimit - 1, bottomLimit - topLimit + 1).ToList();

			float maxScore = playerList[0].Time;
			float minScore = playerList.Last().Time;
			playerList.Reverse();

			List<double> yValues = new List<double>();
			int upperScore;

			if (minScore == maxScore)
			{
				yValues.Add(playerList.Average(p => p.DaggersHit / p.DaggersFired * 100));
			}
			else
			{
				for (float i = minScore; i <= maxScore; i = upperScore)
				{
					upperScore = MoveUp((int)i, bracketSize);
					var toBeAveraged = playerList.Where(p => p.Time >= i && p.Time < upperScore && p.DaggersFired != 0);
					if (!toBeAveraged.Any())
						yValues.Add(0);
					else
						yValues.Add(toBeAveraged.Average(p => (double)p.DaggersHit / p.DaggersFired * 100));
				}
			}

			string labels = GetEveryXLabelFormatted((int)minScore, (int)maxScore, bracketSize);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Avg. accuracy", $"Average accuracy per PB bracket - rank {topLimit}-{bottomLimit}", true);

			return GetStreamFromLink(qc.GetShortUrl());
		}

		private static int MoveUp(int toMove, int nearest) // Always rounds up, even if the number is round
		{
			if (toMove % nearest == 0)
				return toMove + nearest;
			return nearest - toMove % nearest + toMove;
		}

		private static int RoundDown(int toRound, int nearest)
		{
			return toRound - toRound % nearest;
		}

		public static async Task<Stream> GetRoleChartStream(List<string> roleNames, List<int> roleMemberCounts)
		{
			string xData = GetStringFromList(roleNames);
			string yData = GetStringFromList(roleMemberCounts);

			Chart qc = GetChart(xData, yData, "Club role", "Number of members", "Number of members per club role");

			return await GetStreamFromLink(qc.GetShortUrl());
		}

		public static async Task<Stream> GetByDeathChartStream(int bottomLimit, int toplimit)
		{
			LeaderboardData LB = new LeaderboardData();

			if (LB.PlayerList.Count == 0)
				return null;

			var result = LB.PlayerList.Select(p => p.Death)
				.Skip(toplimit - 1)
				.Take(bottomLimit - (toplimit - 1))
				.GroupBy(d => d)
				.OrderBy(t => t.Count());

			string deathLabels = "'" + string.Join("','", result.Select(x => x.Key)) + "'";
			string countLabels = "'" + string.Join("','", result.Select(x => x.Count())) + "'";

			Chart qc = GetChart(deathLabels, countLabels, "Death type", "Number of victims", $"Death type frequency - rank {toplimit}-{bottomLimit}");

			return await GetStreamFromLink(qc.GetShortUrl());
		}

		private static string GetStringFromList<T>(IEnumerable<T> list)
		{
			return "'" + string.Join("','", list) + "'";
		}

		private static async Task<Stream> GetStreamFromLink(string link)
		{
			return await new HttpClient().GetStreamAsync(link);
		}

		private static string GetEveryXLabelFormatted(int minScore, int maxScore, int bracketSize)
		{
			if (minScore == maxScore)
				return $"'{RoundDown(minScore, bracketSize)}-{MoveUp(minScore, bracketSize)}'";

			List<string> labels = new List<string>();

			int cntr = RoundDown(minScore, bracketSize), upper;
			while (cntr <= maxScore)
			{
				upper = MoveUp(cntr, bracketSize);
				if (bracketSize == 1)
					labels.Add($"{cntr}");
				else
					labels.Add($"{cntr}-{upper}");
				cntr = upper;
			}

			return "'" + string.Join("','", labels) + "'";
		}

		private static Chart GetChart(string xData, string yData, string xLabel, string yLabel, string title = null, bool precise = false)
		{
			return new Chart
			{
				Width = 600,
				DevicePixelRatio = 2.0,
				BackgroundColor = "white",
				Config = $@"
            {{
                type: 'bar',
                data: 
                {{
                labels: [{xData}],
                datasets: 
                [
                    {{
					maxBarThickness: 90,
                    backgroundColor: 'darkblue',
                    borderColor: 'darkblue',
                    borderWidth: 1,
                    data: [{yData}]
                    }}
                ]
                }},
                options: {{
                scales: {{
                    xAxes: [
						{{
						scaleLabel: {{
							display: true,
							labelString: '{xLabel}'
						}}
                    }}],
      				yAxes: [
						{{
						ticks: {{
							{(precise ? "" : "precision: 0,")}
							beginAtZero: true
						}},
						scaleLabel: {{
							display: true,
							labelString: '{yLabel}'
						}}
                    }}]
                }},
                legend: false,
                title: {{
                    display: {(title == null ? "false" : "true")},
                    text: '{title}'
                }}
                }}
            }}"
			};
		}
	}
}
