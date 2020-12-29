﻿using Clubber.Databases;
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
				await ReplyAsync($"Property can only be `{string.Join("`/`", GetPropertyStrings())}`.");
				return;
			}
			if (upperLimit > lowerLimit)
			{
				await ReplyAsync("The upper limit can't be bigger than the lower limit.");
				return;
			}

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			granularity = granularity < 1 ? 1 : granularity > 1000 ? 1000 : granularity;
			Stream chartStream = await ChartHelper.GetEveryXBracketChartStream(granularity, Math.Min(MAX_LIMIT, lowerLimit), Math.Max(1, upperLimit), property);

			if (chartStream == null)
			{
				await ReplyAsync("Another operation is in process, try again shortly.");
				return;
			}

			await Context.Channel.SendFileAsync(chartStream, "chart.png");
			await processingMessage.DeleteAsync();
		}

		private static bool PropertyCheck(string property)
		{
			IEnumerable<string> fieldList = GetPropertyStrings();

			foreach (string field in fieldList)
			{
				if (property.Equals(field, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
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
			if (upperLimit > lowerLimit)
			{
				await ReplyAsync("Upper limit can't be bigger than the lower limit.");
				return;
			}

			IUserMessage processingMessage = await ReplyAsync("Processing...");

			Stream chartStream = await ChartHelper.GetByDeathChartStream((int)Math.Max(1, upperLimit), (int)Math.Min(MAX_LIMIT, lowerLimit));

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
