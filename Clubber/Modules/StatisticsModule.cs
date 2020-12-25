using System.Threading.Tasks;
using Discord.Commands;
using Clubber.Helpers;
using Clubber.Databases;
using System.Collections.Generic;
using Discord;
using System.Linq;
using System.IO;
using Discord.WebSocket;

namespace Clubber.Modules
{
	[Name("Statistics")]
	public class StatisticsModule : ModuleBase<SocketCommandContext>
	{
		private readonly ChartHelper ChartHelper;
		private readonly Dictionary<int, ulong> ScoreRolesDict;

		public StatisticsModule(ChartHelper _chartHelper, ScoreRoles _scroreRoles)
		{
			ChartHelper = _chartHelper;
			ScoreRolesDict = _scroreRoles.ScoreRoleDictionary;
		}

		//[Command("every100")]
		//[Summary("Shows number of users per PB bracket that are registered in this bot. For more accurate data run the `byrole` command instead.")]
		public async Task Every100()
		{
			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetEvery100ChartStream();

			await Context.Channel.SendFileAsync(chartStream, "chart.png");
			await processingMessage.DeleteAsync();
		}

		[Command("byrole")]
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

		[Command("bydeath")]
		[Summary("Shows death type frequency within the given top range.")]
		public async Task ByDeath(uint top = 229900)
		{
			if (top < 1)
			{
				await ReplyAsync("What does \"top 0\" mean?");
				return;
			}
			if (top > 229900)
			{
				await ReplyAsync("Can't input a number bigger than 229900.");
				return;
			}

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetByDeathChartStream(top);

			await Context.Channel.SendFileAsync(chartStream, "chart.png");

			await processingMessage.DeleteAsync();
		}
	}
}
