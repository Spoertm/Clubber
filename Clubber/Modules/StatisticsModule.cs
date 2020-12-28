using Clubber.Databases;
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
		private readonly ChartHelper _chartHelper;
		private readonly Dictionary<int, ulong> _scoreRolesDict;

		public StatisticsModule(ChartHelper chartHelper, ScoreRoles scoreRoles)
		{
			_chartHelper = chartHelper;
			_scoreRolesDict = scoreRoles.ScoreRoleDictionary;
		}

		[Command("every10")]
		[Summary("Shows number of users per 10s PB bracket.\n\n`bottomLimit`: The larger number/further down the leaderboard (default = 229900)\n\n`topLimit`: The smaller number/further up the leaderboard (default = 1)")]
		public async Task Every10(uint topLimit, uint bottomLimit)
			=> await EveryX(10, bottomLimit, topLimit);

		[Command("every10")]
		public async Task Every10(uint bottomLimit)
			=> await EveryX(10, bottomLimit, 1);

		[Command("every10")]
		public async Task Every10()
			=> await EveryX(10, _maxLimit, 1);

		[Command("every50")]
		[Summary("Shows number of users per 50s PB bracket.\n\n`bottomLimit`: The larger number/further down the leaderboard (default = 229900)\n\n`topLimit`: The smaller number/further up the leaderboard (default = 1)")]
		public async Task Every50(uint topLimit, uint bottomLimit)
			=> await EveryX(50, bottomLimit, topLimit);

		[Command("every50")]
		public async Task Every50(uint bottomLimit)
			=> await EveryX(50, bottomLimit, 1);

		[Command("every50")]
		public async Task Every50()
			=> await EveryX(50, _maxLimit, 1);

		[Command("every100")]
		[Summary("Shows number of users per 100s PB bracket.\n\n`bottomLimit`: The larger number/further down the leaderboard (default = 229900)\n\n`topLimit`: The smaller number/further up the leaderboard (default = 1)")]
		public async Task Every100(uint topLimit, uint bottomLimit)
			=> await EveryX(100, bottomLimit, topLimit);

		[Command("every100")]
		public async Task Every100(uint bottomLimit)
			=> await EveryX(100, bottomLimit, 1);

		[Command("every100")]
		public async Task Every100()
			=> await EveryX(100, _maxLimit, 1);

		private async Task EveryX(uint bracketSize, uint bottomLimit, uint topLimit)
		{
			topLimit = Math.Max(topLimit, 1);
			bottomLimit = Math.Min(bottomLimit, topLimit - 1);

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await _chartHelper.GetEveryXBracketChartStream(bracketSize, bottomLimit, topLimit);

			if (await IsError(chartStream == null, "Another operation is in process, try again shortly.\n(Or you've input identical limits!)"))
				return;

			await Context.Channel.SendFileAsync(chartStream, "chart.png");
			await processingMessage.DeleteAsync();
		}

		[Command("byrole")]
		[Summary("Shows the number of members per club role.")]
		public async Task ByRole()
		{
			IUserMessage processingMessage = await ReplyAsync("Processing...");

			List<string> roleNames = new();
			List<int> roleMemberCounts = new();
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

		[Command("bydeath")]
		[Summary("Shows death type frequency within the given top range.")]
		public async Task ByDeath(uint topLimit, uint bottomLimit)
		{
			topLimit = Math.Max(topLimit, 1);
			bottomLimit = Math.Min(bottomLimit, topLimit - 1);

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetByDeathChartStream((int)bottomLimit, (int)topLimit);

			if (await IsError(chartStream == null, "Another operation is in process, try again shortly.\n(Or you've input identical limits!)"))
				return;

			await Context.Channel.SendFileAsync(chartStream, "chart.png");
			await processingMessage.DeleteAsync();
		}

		[Command("bydeath")]
		public async Task ByDeath(uint bottomLimit)
			=> await ByDeath(1, bottomLimit);

		[Command("bydeath")]
		public async Task ByDeath()
			=> await ByDeath(1, _maxLimit);
	}
}
