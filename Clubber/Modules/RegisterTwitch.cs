using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Modules;

[Name("Database")]
[Group("linktwitch")]
[Alias("twitch")]
[Summary("Link your Twitch to get integrated into https://ddstats.live/.")]
[RequireContext(ContextType.Guild)]
public class RegisterTwitch : ExtendedModulebase<SocketCommandContext>
{
	private readonly IDatabaseHelper _databaseHelper;
	private readonly UserService _userService;

	public RegisterTwitch(IDatabaseHelper databaseHelper, UserService userService)
	{
		_databaseHelper = databaseHelper;
		_userService = userService;
	}

	[Command]
	[Remarks("twitch ClubberTTV")]
	[Priority(1)]
	public async Task RegisterSelf([Name("Twitch username")] string twitchUsername)
		=> await CheckUserAndRegisterTwitch((Context.User as SocketGuildUser)!, twitchUsername, true);

	[Command]
	[Remarks("twitch ClubberTTV clubber\nlinktwitch ClubberTTV <@743431502842298368>")]
	[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
	[Priority(2)]
	public async Task RegisterByName([Name("Twitch username")] string twitchUsername, [Name("name | tag")][Remainder] string name)
	{
		(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
		if (success && user is not null)
			await CheckUserAndRegisterTwitch(user, twitchUsername);
	}

	[Command("id")]
	[Remarks("twitch id ClubberTTV 743431502842298368")]
	[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
	[Priority(3)]
	public async Task RegisterByDiscordId([Name("Twitch username")] string twitchUsername, [Name("Discord ID")] ulong discordId)
	{
		(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
		if (success && user is not null)
			await CheckUserAndRegisterTwitch(user, twitchUsername);
	}

	private async Task CheckUserAndRegisterTwitch(IGuildUser user, string twitchUsername, bool selfCommand = false)
	{
		(bool isError, string? message) = _userService.IsBotOrCheater(user, selfCommand);
		if (isError && message is not null)
		{
			await InlineReplyAsync(message);
			return;
		}

		if (await _databaseHelper.TwitchUsernameIsRegistered(twitchUsername))
		{
			await InlineReplyAsync("That Twitch username is already registered.");
			return;
		}

		(bool success, string registerResponseMessage) = await _databaseHelper.RegisterTwitch(user.Id, twitchUsername);
		if (success)
			await InlineReplyAsync("✅ Successfully linked Twitch.");
		else
			await InlineReplyAsync($"Failed to execute command: {registerResponseMessage}");
	}
}
