using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.DdRoleUpdater
{
	public static class Helper
	{
		public static DdUser GetDdUserFromId(ulong discordId, IMongoCollection<DdUser> collection) => collection.Find(x => x.DiscordId == discordId).SingleOrDefault();

		public static bool DiscordIdExistsInDb(ulong discordId, IMongoCollection<DdUser> collection) => collection.Find(x => x.DiscordId == discordId).Any();

		public static bool LeaderboardIdExistsInDb(int lbId, IMongoCollection<DdUser> collection) => collection.Find(x => x.LeaderboardId == lbId).Any();

		public static bool MemberHasRole(SocketGuildUser member, ulong roleId) => member.Roles.Any(role => role.Id == roleId);

		public static string GetCommandAndParameterString(CommandInfo cmd) // Returns the command and its params in the format: commandName <requiredParam> [optionalParam]
=> $"{cmd.Name} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**[{p.Name}]**" : $"**[{p.Name} = {p.DefaultValue}]**" : $"**<{p.Name}>**"))}";

		public static async Task AddToDbFromName(IEnumerable<SocketGuildUser> userMatches, string name, uint rankOrLbId, Func<uint, ulong, Task> asyncCommand, ISocketMessageChannel channel)
		{
			int numberOfMatches = userMatches.Count();
			var converted = ulong.TryParse(name, out ulong discordId);
			if (numberOfMatches == 0)
			{
				if (!converted) { await channel.SendMessageAsync($"Failed to find user with the name `{name}`."); return; }
				await asyncCommand(rankOrLbId, discordId);
			}
			else if (numberOfMatches == 1) await asyncCommand(rankOrLbId, userMatches.First().Id);
			else await channel.SendMessageAsync($"Multiple people have the name `{name.ToLower()}`. Try mentioning the user.");
		}
	}
}
