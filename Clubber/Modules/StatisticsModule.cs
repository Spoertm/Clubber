using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clubber.Databases;
using Clubber.Files;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Modules
{
	[Name("Statistics")]
	public class StatisticsModule : ModuleBase<SocketCommandContext>
	{
		private readonly ChartHelper ChartHelper;
		private readonly Dictionary<int, ulong> ScoreRolesDict;
		private const int MAX_LIMIT = 229900;
		private const int DEFAULT_UPPER_LIMIT = 1;
		private const int DEFAULT_GRANULARITY = 10;
		private const string DEFAULT_PROPERTYTYPE = PropertyType.Seconds;

		public StatisticsModule(ChartHelper _chartHelper, ScoreRoles _scroreRoles)
		{
			ChartHelper = _chartHelper;
			ScoreRolesDict = _scroreRoles.ScoreRoleDictionary;
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
			=> await GraphX(property, (int)granularity, DEFAULT_UPPER_LIMIT, (int)lowerLimit);

		[Command("graph")]
		public async Task Graph(string property, uint granularity)
			=> await GraphX(property, (int)granularity, DEFAULT_UPPER_LIMIT, MAX_LIMIT);

		[Command("graph")]
		public async Task Graph(string property)
			=> await GraphX(property, DEFAULT_GRANULARITY, DEFAULT_UPPER_LIMIT, MAX_LIMIT);

		[Command("graph")]
		public async Task Graph()
			=> await GraphX(DEFAULT_PROPERTYTYPE, DEFAULT_GRANULARITY, DEFAULT_UPPER_LIMIT, MAX_LIMIT);

		private async Task GraphX(string property, int granularity, int upperLimit, int lowerLimit)
		{
			if (!PropertyCheck(property))
			{
				await ReplyAsync("Property can only be `seconds`/`kills`/`gems`/`daggershit`/`daggersfired`/`daggersperc`");
				return;
			}
			if (granularity < 1 || granularity > 1000)
			{
				await ReplyAsync("Granularity can't be smaller than 1 or greater than 1000.");
				return;
			}
			if (upperLimit < 1 || lowerLimit > MAX_LIMIT)
			{
				await ReplyAsync("Upper limit can't be smaller than 1 and lower limit can't be greater than 229900.");
				return;
			}
			if (upperLimit > lowerLimit)
			{
				await ReplyAsync("The upper limit can't be bigger than the lower limit.");
				return;
			}



			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetEveryXBracketChartStream(granularity, lowerLimit, upperLimit, property);

			if (chartStream == null)
			{
				await ReplyAsync("Another operation is in process, try again shortly.");
				return;
			}

			await Context.Channel.SendFileAsync(chartStream, "chart.png");
			await processingMessage.DeleteAsync();
		}

		private bool PropertyCheck(string property)
		{
			IEnumerable<string> fieldList = new PropertyType()
				.GetType()
				.GetFields()
				.Select(f => f.GetRawConstantValue() as string);

			foreach (string field in fieldList)
			{
				if (property.Equals(field, System.StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		[Command("rolegraph")]
		[Summary("Shows the number of members per club role.")]
		public async Task ByRole()
		{
			IUserMessage processingMessage = await ReplyAsync("Processing...");

			List<string> roleNames = new List<string>();
			List<int> roleMemberCounts = new List<int>();
			SocketRole role;

			foreach (var id in ScoreRolesDict.Values.Reverse())
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
			if (upperLimit < 1 || lowerLimit > 229900)
			{
				await ReplyAsync("Upper limit can't be smaller than 1 and the lower limit can't be bigger than 229900.");
				return;
			}
			if (upperLimit > lowerLimit)
			{
				await ReplyAsync("Upper limit can't be bigger than the lower limit.");
				return;
			}

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetByDeathChartStream((int)lowerLimit, (int)upperLimit);

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
			=> await DeathGraph(1, MAX_LIMIT);
	}
}
