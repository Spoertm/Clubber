using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Clubber.Files;
using System.Linq;
using Clubber.Databases;
using QuickChart;
using MongoDB.Driver;

namespace Clubber.Helpers
{
	public class ChartHelper
	{
		private readonly IMongoCollection<DdUser> Collection;

		public ChartHelper(MongoDatabase _database)
		{
			Collection = _database.DdUserCollection;
		}

		public async Task<Stream> GetEvery100ChartStream()
		{
			List<int> yValues = new List<int>();
			for (int i = 0; i < 1200; i += 100)
			{
				yValues.Add(Collection.AsQueryable().ToList().FindAll(du => du.Score >= i && du.Score < i + 100).Count);
			}

			string labels = GetEvery100LabelFormatted(0, 1199);
			string data = GetStringFromList(yValues);
			Chart qc = GetChart(labels, data, "PB bracket", "Number of users", "Number of users in DD Pals based on PB within range");

			return await GetStreamFromLink(qc.GetUrl());
		}

		public static async Task<Stream> GetRoleChartStream(List<string> roleNames, List<int> roleMemberCounts)
		{
			string xData = GetStringFromList(roleNames);
			string yData = GetStringFromList(roleMemberCounts);

			Chart qc = GetChart(xData, yData, "Club role", "Number of members", "Number of members per club role");

			return await GetStreamFromLink(qc.GetUrl());
		}

		public static async Task<Stream> GetByDeathChartStream(uint top)
		{
			LeaderboardData LB = new LeaderboardData(top);

			var result = LB.Deaths.GroupBy(d => d).OrderBy(t => t.Count());

			string deathLabels = "'" + string.Join("','", result.Select(x => x.Key)) + "'";
			string countLabels = "'" + string.Join("','", result.Select(x => x.Count())) + "'";

			Chart qc = GetChart(deathLabels, countLabels, "Death type", "Number of victims", $"Death type frequency - {(top == 229900 ? "entire LB" : $"top {top}")}");

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

		private static string GetEvery100LabelFormatted(int start, int limit)
		{
			List<string> labels = new List<string>
			{
				$"{start}-{start + 99}"
			};

			for (int i = start + 99; i < limit; i += 100)
			{
				labels.Add($"{i + 1}-{i + 100}");
			}

			return "'" + string.Join("','", labels) + "'";
		}

		private static Chart GetChart(string xData, string yData, string xLabel, string yLabel, string title = null)
		{
			return new Chart
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
                    yAxes: [{{
						ticks: {{
							stepSize: 1,
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
