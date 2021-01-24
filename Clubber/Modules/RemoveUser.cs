using Clubber.Files;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Database")]
	[Group("remove")]
	[Summary("Removes a user from the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public class RemoveUser : AbstractModule<SocketCommandContext>
	{
		[Command("id")]
		[Priority(2)]
		public async Task RemoveByDiscordId(ulong discordId)
		{
			List<DdUser> list = DatabaseHelper.DdUsers;
			DdUser? toRemove = list.Find(du => du.DiscordId == discordId);
			if (toRemove != null)
			{
				list.Remove(toRemove);
				await DatabaseHelper.UpdateDbFile(list, $"Remove-{discordId}-{toRemove.LeaderboardId}");
				await ReplyAsync("✅ Successfully removed.");
			}
			else
			{
				await ReplyAsync("User not registered.");
			}
		}

		[Command]
		[Priority(1)]
		public async Task RemoveByName([Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user != null)
			{
				if (await DatabaseHelper.RemoveUser(user))
					await ReplyAsync("✅ Successfully removed.");
				else
					await ReplyAsync("User not registered.");
			}
		}
	}
}
