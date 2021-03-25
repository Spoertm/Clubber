using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Database")]
	[Group("remove")]
	[Summary("Removes a user from the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	[RequireContext(ContextType.Guild)]
	public class RemoveUser : ExtendedModulebase<SocketCommandContext>
	{
		private readonly DatabaseHelper _databaseHelper;

		public RemoveUser(DatabaseHelper databaseHelper)
		{
			_databaseHelper = databaseHelper;
		}

		[Command]
		[Remarks("remove clubber\nremove <@743431502842298368>")]
		[Priority(1)]
		public async Task RemoveByName([Name("name | tag")][Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user is not null)
			{
				if (await _databaseHelper.RemoveUser(user))
					await InlineReplyAsync("✅ Successfully removed.");
				else
					await InlineReplyAsync("User not registered to begin with.");
			}
		}

		[Command("id")]
		[Remarks("remove id 743431502842298368")]
		[Priority(2)]
		public async Task RemoveByDiscordId([Name("Discord ID")] ulong discordId)
		{
			if (await _databaseHelper.RemoveUser(discordId))
				await InlineReplyAsync("✅ Successfully removed.");
			else
				await InlineReplyAsync("No such ID found.");
		}
	}
}
