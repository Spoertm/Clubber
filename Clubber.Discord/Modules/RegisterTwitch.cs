using Clubber.Discord.Services;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Discord.Modules;

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
	[Remarks("twitch ClubberTTV clubber\ntwitch ClubberTTV <@743431502842298368>")]
	[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
	[Priority(2)]
	public async Task RegisterByName([Name("Twitch username")] string twitchUsername, [Name("name | tag")][Remainder] string name)
	{
		Result<SocketGuildUser> result = await FoundOneUserFromName(name);
		if (result.IsSuccess)
			await CheckUserAndRegisterTwitch(result.Value, twitchUsername);
	}

	[Command("id")]
	[Remarks("twitch id ClubberTTV 743431502842298368")]
	[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
	[Priority(3)]
	public async Task RegisterByDiscordId([Name("Twitch username")] string twitchUsername, [Name("Discord ID")] ulong discordId)
	{
		Result<SocketGuildUser> result = await FoundUserFromDiscordId(discordId);
		if (result.IsSuccess)
			await CheckUserAndRegisterTwitch(result.Value, twitchUsername);
	}

	private async Task CheckUserAndRegisterTwitch(IGuildUser user, string twitchUsername, bool selfCommand = false)
	{
		Result result = _userService.IsNotBotOrCheater(user, selfCommand);
		if (result.IsFailure)
		{
			await InlineReplyAsync(result.ErrorMsg);
			return;
		}

		if (await _databaseHelper.TwitchUsernameIsRegistered(twitchUsername))
		{
			await InlineReplyAsync("That Twitch username is already registered.");
			return;
		}

		Result registrationResult = await _databaseHelper.RegisterTwitch(user.Id, twitchUsername);
		if (registrationResult.IsSuccess)
		{
			await InlineReplyAsync("✅ Successfully linked Twitch.");
		}
		else
		{
			await InlineReplyAsync($"Failed to execute command: {registrationResult.ErrorMsg}");
		}
	}
}
