using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Discord.Modules;

[Name("Database")]
[Group("unregister")]
[Alias("remove")]
[Summary("Removes a user from the database.")]
[RequireUserPermission(GuildPermission.ManageRoles)]
[RequireContext(ContextType.Guild)]
public class RemoveUser : ExtendedModulebase<SocketCommandContext>
{
	private readonly IDatabaseHelper _databaseHelper;

	public RemoveUser(IDatabaseHelper databaseHelper)
	{
		_databaseHelper = databaseHelper;
	}

	[Command]
	[Remarks("+unregister clubber\nunregister <@743431502842298368>")]
	[Priority(1)]
	public async Task RemoveByName([Name("name | tag")][Remainder] string name)
	{
		Result<SocketGuildUser> result = await FoundOneUserFromName(name);
		if (result.IsSuccess)
		{
			if (await _databaseHelper.RemoveUser(result.Value.Id))
			{
				await InlineReplyAsync("✅ Successfully removed.");
			}
			else
			{
				await InlineReplyAsync("User not registered to begin with.");
			}
		}
	}

	[Command("id")]
	[Remarks("unregister id 743431502842298368")]
	[Priority(2)]
	public async Task RemoveByDiscordId([Name("Discord ID")] ulong discordId)
	{
		if (await _databaseHelper.RemoveUser(discordId))
		{
			await InlineReplyAsync("✅ Successfully removed.");
		}
		else
		{
			await InlineReplyAsync("No such ID found.");
		}
	}
}
