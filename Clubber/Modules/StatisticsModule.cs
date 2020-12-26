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
		private const int MAX_LIMIT = 229900;

		public StatisticsModule(ChartHelper _chartHelper, ScoreRoles _scroreRoles)
		{
			ChartHelper = _chartHelper;
			ScoreRolesDict = _scroreRoles.ScoreRoleDictionary;
		}

		[Command("every10")]
		[Summary("Shows number of users per 10s PB bracket.\n\n`bottomLimit`: The larger number/further down the leaderboard (default = 229900)\n\n`topLimit`: The smaller number/further up the leaderboard (default = 1)")]
		public async Task Every10(uint topLimit, uint bottomLimit)
			=> await EveryX(10, (int)bottomLimit, (int)topLimit);

		[Command("every10")]
		public async Task Every10(uint bottomLimit)
			=> await EveryX(10, (int)bottomLimit, 1);

		[Command("every10")]
		public async Task Every10()
			=> await EveryX(10, MAX_LIMIT, 1);


		[Command("every50")]
		[Summary("Shows number of users per 50s PB bracket.\n\n`bottomLimit`: The larger number/further down the leaderboard (default = 229900)\n\n`topLimit`: The smaller number/further up the leaderboard (default = 1)")]
		public async Task Every50(uint topLimit, uint bottomLimit)
			=> await EveryX(50, (int)bottomLimit, (int)topLimit);

		[Command("every50")]
		public async Task Every50(uint bottomLimit)
			=> await EveryX(50, (int)bottomLimit, 1);

		[Command("every50")]
		public async Task Every50()
			=> await EveryX(50, MAX_LIMIT, 1);


		[Command("every100")]
		[Summary("Shows number of users per 100s PB bracket.\n\n`bottomLimit`: The larger number/further down the leaderboard (default = 229900)\n\n`topLimit`: The smaller number/further up the leaderboard (default = 1)")]
		public async Task Every100(uint topLimit, uint bottomLimit)
			=> await EveryX(100, (int)bottomLimit, (int)topLimit);

		[Command("every100")]
		public async Task Every100(uint bottomLimit)
			=> await EveryX(100, (int)bottomLimit, 1);

		[Command("every100")]
		public async Task Every100()
			=> await EveryX(100, 229900, 1);

		private async Task EveryX(int bracketSize, int bottomLimit, int topLimit)
		{
			if (bottomLimit < topLimit)
			{
				await ReplyAsync("The bottom limit cant be smaller than the upper limit.");
				return;
			}
			if (bottomLimit > MAX_LIMIT || topLimit < 1)
			{
				await ReplyAsync("Bottom limit can't be larger than 229900 and top limit can't be smaller than 1.");
				return;
			}

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetEveryXBracketChartStream(bracketSize, bottomLimit, topLimit);

			if (chartStream == null)
			{
				await ReplyAsync("Another operation is in process, try again shortly.\n(Or you've input identical limits!)");
				return;
			}

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
		public async Task ByDeath(uint bottomLimit = 229900, uint topLimit = 1)
		{
			if (bottomLimit < topLimit)
			{
				await ReplyAsync("The bottom limit cant be smaller than the upper limit.");
				return;
			}
			if (bottomLimit > 229900 || topLimit < 1)
			{
				await ReplyAsync("Bottom limit can't be larger than 229900 and top limit can't be smaller than 1.");
				return;
			}

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetByDeathChartStream((int)bottomLimit, (int)topLimit);

			if (chartStream == null)
			{
				await ReplyAsync("Another operation is in process, try again shortly.\n(Or you've input identical limits!)");
				return;
			}

			await Context.Channel.SendFileAsync(chartStream, "chart.png");
			await processingMessage.DeleteAsync();
		}
	}
}
