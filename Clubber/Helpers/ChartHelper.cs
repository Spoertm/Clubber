using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Clubber.Files;
using System.Linq;
using QuickChart;
using MongoDB.Driver;

namespace Clubber.Helpers
{
	public class ChartHelper
	{
		public async Task<Stream> GetEveryXBracketChartStream(int bracketSize, int bottomLimit, int toplimit)
		{
			LeaderboardData LB = new LeaderboardData();

			if (LB.Times.Count == 0)
				return null;

			List<int> times = LB.Times.Select(t => t / 10000).ToList();

			int roundedDown = RoundDown(times[bottomLimit - 1], bracketSize);

			times = times.Skip(toplimit - 1).TakeWhile(t => t >= roundedDown).ToList();

			int maxtime = times[0];

			List<int> yValues = new List<int>();
			for (int i = roundedDown; i < maxtime; i += bracketSize)
			{
				yValues.Add(times.FindAll(t => t >= i && t < i + bracketSize).Count);
			}

			string labels = GetEveryXLabelFormatted(roundedDown, maxtime, bracketSize);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Number of players", $"Number of players within a PB bracket - rank {toplimit}-{bottomLimit}");

			return await GetStreamFromLink(qc.GetUrl());
		}

		private static int RoundUp(int toRound, int nearest)
		{
			if (toRound % nearest == 0) return toRound;
			return (nearest - toRound % nearest) + toRound;
		}

		private static int RoundDown(int toRound, int nearest) => toRound - toRound % nearest;

		public static async Task<Stream> GetRoleChartStream(List<string> roleNames, List<int> roleMemberCounts)
		{
			string xData = GetStringFromList(roleNames);
			string yData = GetStringFromList(roleMemberCounts);

			Chart qc = GetChart(xData, yData, "Club role", "Number of members", "Number of members per club role");

			return await GetStreamFromLink(qc.GetUrl());
		}

		public static async Task<Stream> GetByDeathChartStream(int bottomLimit, int toplimit)
		{
			LeaderboardData LB = new LeaderboardData();

			if (LB.Times.Count == 0)
				return null;

			var result = LB.Deaths
				.Skip(toplimit - 1)
				.Take(bottomLimit - (toplimit - 1))
				.GroupBy(d => d)
				.OrderBy(t => t.Count());

			string deathLabels = "'" + string.Join("','", result.Select(x => x.Key)) + "'";
			string countLabels = "'" + string.Join("','", result.Select(x => x.Count())) + "'";

			Chart qc = GetChart(deathLabels, countLabels, "Death type", "Number of victims", $"Death type frequency - rank {toplimit}-{bottomLimit}");

			return await GetStreamFromLink(qc.GetUrl());
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
			List<string> labels = new List<string>
			{
				$"{minScore}-{minScore + (bracketSize - 1)}"
			};

			for (int i = minScore + (bracketSize - 1); i < maxScore; i += bracketSize)
			{
				labels.Add($"{i + 1}-{i + bracketSize}");
			}

			return "'" + string.Join("','", labels) + "'";
		}

		private static Chart GetChart(string xData, string yData, string xLabel, string yLabel, string title = null)
		{
			Chart qc = new Chart
			{
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
							precision: 0,
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

			return qc;
		}
	}
}
