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
		[Command]
		[Remarks("remove clubber\nremove <@743431502842298368>")]
		[Priority(1)]
		public async Task RemoveByName([Name("name | tag")][Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user != null)
			{
				if (await DatabaseHelper.RemoveUser(user))
					await InlineReplyAsync("✅ Successfully removed.");
				else
					await InlineReplyAsync("User not registered.");
			}
		}

		[Command("id")]
		[Remarks("remove id 743431502842298368")]
		[Priority(2)]
		public async Task RemoveByDiscordId([Name("Discord ID")] ulong discordId)
		{
			List<DdUser> list = DatabaseHelper.DdUsers;
			DdUser? toRemove = list.Find(du => du.DiscordId == discordId);
			if (toRemove != null)
			{
				list.Remove(toRemove);
				await DatabaseHelper.UpdateDbFile(list, $"Remove-{discordId}-{toRemove.LeaderboardId}");
				await InlineReplyAsync("✅ Successfully removed.");
			}
			else
			{
				await InlineReplyAsync("No such ID found.");
			}
		}
	}
}
