using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Files
{
	public static class Helper
	{
		public static DdUser GetDdUserFromId(ulong discordId, IMongoCollection<DdUser> collection) => collection.Find(x => x.DiscordId == discordId).SingleOrDefault();

		public static bool DiscordIdExistsInDb(ulong discordId, IMongoCollection<DdUser> collection) => collection.Find(x => x.DiscordId == discordId).Any();

		public static bool LeaderboardIdExistsInDb(int lbId, IMongoCollection<DdUser> collection) => collection.Find(x => x.LeaderboardId == lbId).Any();

		public static bool MemberHasRole(SocketGuildUser member, ulong roleId) => member.Roles.Any(role => role.Id == roleId);

		public static async Task AddToDbFromName(IEnumerable<SocketGuildUser> userMatches, string name, uint rankOrLbId, Func<uint, ulong, Task> asyncCommand, ISocketMessageChannel channel)
		{
			int numberOfMatches = userMatches.Count();
			if (numberOfMatches == 0) await channel.SendMessageAsync($"No user found.");
			else if (numberOfMatches == 1) await asyncCommand(rankOrLbId, userMatches.First().Id);
			else await channel.SendMessageAsync($"Multiple people have the name `{name.ToLower()}`. Try mentioning the user.");
		}
	}
}
