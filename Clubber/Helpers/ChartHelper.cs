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
		public async Task<Stream> GetEveryXBracketChartStream(uint bracketSize, uint bottomLimit, uint toplimit)
		{
			LeaderboardData lb = new();

			if (lb.Times.Count == 0)
				return null;

			List<uint> times = lb.Times.Select(t => t / 10000).ToList();

			uint roundedDown = RoundDown(times[(int)(bottomLimit - 1)], bracketSize);

			times = times.Skip((int)(toplimit - 1)).TakeWhile(t => t >= roundedDown).ToList();

			uint maxtime = times[0];

			List<int> yValues = new();
			for (uint i = roundedDown; i < maxtime; i += bracketSize)
				yValues.Add(times.FindAll(t => t >= i && t < i + bracketSize).Count);

			string labels = GetEveryXLabelFormatted(roundedDown, maxtime, bracketSize);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Number of players", $"Number of players within a PB bracket - rank {toplimit}-{bottomLimit}");

			return await GetStreamFromLink(qc.GetUrl());
		}

		private static uint RoundDown(uint toRound, uint nearest)
			=> toRound - toRound % nearest;

		public static async Task<Stream> GetRoleChartStream(List<string> roleNames, List<int> roleMemberCounts)
		{
			string xData = GetStringFromList(roleNames);
			string yData = GetStringFromList(roleMemberCounts);

			Chart qc = GetChart(xData, yData, "Club role", "Number of members", "Number of members per club role");

			return await GetStreamFromLink(qc.GetUrl());
		}

		public static async Task<Stream> GetByDeathChartStream(int bottomLimit, int toplimit)
		{
			LeaderboardData lb = new();

			if (lb.Times.Count == 0)
				return null;

			var result = lb.Deaths
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

		private static string GetEveryXLabelFormatted(uint minScore, uint maxScore, uint bracketSize)
		{
			List<string> labels = new()
			{
				$"{minScore}-{minScore + (bracketSize - 1)}"
			};

			for (uint i = minScore + (bracketSize - 1); i < maxScore; i += bracketSize)
				labels.Add($"{i + 1}-{i + bracketSize}");

			return GetStringFromList(labels);
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
