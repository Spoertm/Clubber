using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Discord.Modules;

[Name("Database")]
[Group("unlinktwitch")]
[Alias("unlinktwitch")]
[Summary("Unlink your Twitch account.")]
[RequireContext(ContextType.Guild)]
public class UnregisterTwitch : ExtendedModulebase<SocketCommandContext>
{
	private readonly IDatabaseHelper _databaseHelper;

	public UnregisterTwitch(IDatabaseHelper databaseHelper)
	{
		_databaseHelper = databaseHelper;
	}

	[Command]
	[Remarks("unlinktwitch ClubberTTV")]
	[Priority(1)]
	public async Task UnregisterSelf()
		=> await CheckUserAndUnregisterTwitch((Context.User as SocketGuildUser)!);

	[Command]
	[Remarks("unlinktwitch ClubberTTV clubber\nunlinktwitch ClubberTTV <@743431502842298368>")]
	[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
	[Priority(2)]
	public async Task UnregisterByName([Name("name | tag")][Remainder] string name)
	{
		Result<SocketGuildUser> result = await FoundOneUserFromName(name);
		if (result.IsSuccess)
		{
			await CheckUserAndUnregisterTwitch(result.Value);
		}
	}

	[Command("id")]
	[Remarks("unlinktwitch id ClubberTTV 743431502842298368")]
	[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
	[Priority(3)]
	public async Task UnregisterByDiscordId([Name("Discord ID")] ulong discordId)
	{
		Result<SocketGuildUser> result = await FoundUserFromDiscordId(discordId);
		if (result.IsSuccess)
		{
			await CheckUserAndUnregisterTwitch(result.Value);
		}
	}

	private async Task CheckUserAndUnregisterTwitch(IGuildUser user)
	{
		Result result = await _databaseHelper.UnregisterTwitch(user.Id);
		if (result.IsSuccess)
		{
			await InlineReplyAsync("✅ Successfully unlinked Twitch account.");
		}
		else
		{
			await InlineReplyAsync($"Failed to execute command: {result.ErrorMsg}");
		}
	}
}
