using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Modules;

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
		(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
		if (success && user is not null)
			await CheckUserAndUnregisterTwitch(user);
	}

	[Command("id")]
	[Remarks("unlinktwitch id ClubberTTV 743431502842298368")]
	[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
	[Priority(3)]
	public async Task UnregisterByDiscordId([Name("Discord ID")] ulong discordId)
	{
		(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
		if (success && user is not null)
			await CheckUserAndUnregisterTwitch(user);
	}

	private async Task CheckUserAndUnregisterTwitch(IGuildUser user)
	{
		(bool success, string errorMsg) = await _databaseHelper.UnregisterTwitch(user.Id);
		if (success)
			await InlineReplyAsync("✅ Successfully unlinked Twitch account.");
		else
			await InlineReplyAsync($"Failed to execute command: {errorMsg}");
	}
}
