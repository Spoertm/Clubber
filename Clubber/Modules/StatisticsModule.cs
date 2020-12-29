using Clubber.Databases;
using Clubber.Files;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Statistics")]
	public class StatisticsModule : AbstractModule<SocketCommandContext>
	{
		private const int _maxLimit = 229900;
		private const int _defaultUpperLimit = 1;
		private const int _defaultGranularity = 10;
		private const string _defaultPropertyType = PropertyType.Seconds;

		private readonly ChartHelper _chartHelper;
		private readonly Dictionary<int, ulong> _scoreRolesDict;

		public StatisticsModule(ChartHelper chartHelper, ScoreRoles scoreRoles)
		{
			_chartHelper = chartHelper;
			_scoreRolesDict = scoreRoles.ScoreRoleDictionary;
		}

		[Command("graph")]
		[Summary(@"
Shows a graph of how frequent a property occurs within the DD leaderboard.

`property`: Could be seconds/kills/gems/daggershit/daggersfired/accuracy (default = seconds)
`granularity`: How roughly detailed the graph should be, in seconds (default = 10)
`upperLimit`: The rank where the range starts (default = 1)
`lowerLimit`: The rank where the range ends (default = 229900, also the maximum)")]
		public async Task Graph(string property, uint granularity, uint upperLimit, uint lowerLimit)
			=> await GraphX(property, (int)granularity, (int)upperLimit, (int)lowerLimit);

		[Command("graph")]
		public async Task Graph(string property, uint granularity, uint lowerLimit)
			=> await GraphX(property, (int)granularity, _defaultUpperLimit, (int)lowerLimit);

		[Command("graph")]
		public async Task Graph(string property, uint granularity)
			=> await GraphX(property, (int)granularity, _defaultUpperLimit, _maxLimit);

		[Command("graph")]
		public async Task Graph(string property)
			=> await GraphX(property, _defaultGranularity, _defaultUpperLimit, _maxLimit);

		[Command("graph")]
		public async Task Graph()
			=> await GraphX(_defaultPropertyType, _defaultGranularity, _defaultUpperLimit, _maxLimit);

		private async Task GraphX(string property, int granularity, int upperLimit, int lowerLimit)
		{
			if (await IsError(!PropertyCheck(property), $"Property can only be `{string.Join("`/`", GetPropertyStrings())}`."))
				return;

			if (await IsError(upperLimit > lowerLimit, "The upper limit can't be bigger than the lower limit."))
				return;

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			granularity = granularity < 1 ? 1 : granularity > 1000 ? 1000 : granularity;
			Stream chartStream = await _chartHelper.GetEveryXBracketChartStream(granularity, Math.Min(_maxLimit, lowerLimit), Math.Max(1, upperLimit), property);

			if (await IsError(chartStream == null, "Another operation is in process, try again shortly."))
				return;

			await Context.Channel.SendFileAsync(chartStream, "chart.png");
			await processingMessage.DeleteAsync();
		}

		private static bool PropertyCheck(string property)
		{
			foreach (string field in GetPropertyStrings())
			{
				if (property.Equals(field, StringComparison.InvariantCultureIgnoreCase))
					return true;
			}

			return false;
		}

		private static IEnumerable<string> GetPropertyStrings()
		{
			return new PropertyType()
				.GetType()
				.GetFields()
				.Select(f => f.GetRawConstantValue() as string);
		}

		[Command("rolegraph")]
		[Summary("Shows the number of members per club role.")]
		public async Task ByRole()
		{
			IUserMessage processingMessage = await ReplyAsync("Processing...");

			List<string> roleNames = new List<string>();
			List<int> roleMemberCounts = new List<int>();
			SocketRole role;

			foreach (ulong id in _scoreRolesDict.Values.Reverse())
			{
				role = Context.Guild.GetRole(id);
				roleNames.Add(role.Name);
				roleMemberCounts.Add(role.Members.Count());
			}

			Stream chartStream = await ChartHelper.GetRoleChartStream(roleNames, roleMemberCounts);

			await Context.Channel.SendFileAsync(chartStream, "chart.png");

			await processingMessage.DeleteAsync();
		}

		[Command("deathgraph")]
		[Summary("Shows death type frequency within the given top range.")]
		public async Task DeathGraph(uint upperLimit, uint lowerLimit)
		{
			if (upperLimit > lowerLimit)
			{
				await ReplyAsync("Upper limit can't be bigger than the lower limit.");
				return;
			}

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetByDeathChartStream((int)Math.Max(1, upperLimit), (int)Math.Min(_maxLimit, lowerLimit));

			if (chartStream == null)
			{
				await ReplyAsync("Another operation is in process, try again shortly.");
				return;
			}

			await Context.Channel.SendFileAsync(chartStream, "chart.png");
			await processingMessage.DeleteAsync();
		}

		[Command("deathgraph")]
		public async Task ByDeath(uint lowerLimit)
			=> await DeathGraph(1, lowerLimit);

		[Command("deathgraph")]
		public async Task ByDeath()
			=> await DeathGraph(1, _maxLimit);
	}
}
