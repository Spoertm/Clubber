using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Clubber.DdRoleUpdater
{
	public static class Helper
	{
		private static readonly string DbJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/DdPlayerDataBase.json");

		public static Dictionary<ulong, DdUser> DeserializeDb() => JsonConvert.DeserializeObject<Dictionary<ulong, DdUser>>(File.ReadAllText(DbJsonPath));

		public static DdUser GetDdUserFromId(ulong discordId) => DeserializeDb()[discordId];

		public static bool IsValidDiscordId(ulong discordId, IReadOnlyCollection<SocketGuildUser> guildUsers) => guildUsers.Any(u => u.Id == discordId);

		public static bool DiscordIdExistsInDb(ulong discordId) => DeserializeDb().ContainsKey(discordId);

		public static bool LeaderboardIdExistsInDb(int lbId) => DeserializeDb().Values.Any(v => v.LeaderboardId == lbId);

		public static bool MemberHasRole(SocketGuildUser member, ulong roleId) => member.Roles.Any(role => role.Id == roleId);

		public static string GetCommandAndParameterString(CommandInfo cmd) // Returns the command and its params in the format: commandName <requiredParam> [optionalParam]
=> $"{cmd.Name} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**[{p.Name}]**" : $"**[{p.Name} = {p.DefaultValue}]**" : $"**<{p.Name}>**"))}";

		public static async Task AddToDbFromName(IEnumerable<SocketGuildUser> userMatches, string name, uint num, Func<uint, ulong, Task> asyncCommand, ISocketMessageChannel channel)
		{
			int numberOfMatches = userMatches.Count();
			if (numberOfMatches == 0) await channel.SendMessageAsync($"Found no user(s) with the username/nickname `{name}`.");
			else if (numberOfMatches == 1) await asyncCommand(num, userMatches.First().Id);
			else await channel.SendMessageAsync($"Multiple people have the name {name.ToLower()}. Please specify a username or mention the user instead.");
		}

		public static async Task ExecuteFromName(IEnumerable<SocketGuildUser> userMatches, string name, Func<string, Task> asyncCommand, ISocketMessageChannel channel)
		{

			int numberOfMatches = userMatches.Count();
			if (numberOfMatches == 0) await channel.SendMessageAsync($"Found no user(s) with the username/nickname `{name.ToLower()}`.");
			else if (numberOfMatches == 1) await asyncCommand(userMatches.First().Mention);
			else await channel.SendMessageAsync($"Multiple people have the name {name.ToLower()}. Please specify a username or mention the user instead.");
		}
	}
}
