using Clubber.Helpers;
using Clubber.Models.Responses;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Modules;

[Name("Roles")]
[Group("pb")]
[Alias("updateroles")]
[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles.")]
[RequireContext(ContextType.Guild)]
public class UpdateRoles : ExtendedModulebase<SocketCommandContext>
{
	private readonly UpdateRolesHelper _updateRolesHelper;
	private readonly UserService _userService;

	public UpdateRoles(UpdateRolesHelper updateRolesHelper, UserService userService)
	{
		_updateRolesHelper = updateRolesHelper;
		_userService = userService;
	}

	[Command]
	[Remarks("pb")]
	[Priority(1)]
	public async Task UpdateRolesFromCurrentUser() => await CheckUserAndUpdateRoles(Context.Guild.GetUser(Context.User.Id));

	[Command]
	[Remarks("pb clubber\npb <@743431502842298368>")]
	[Priority(2)]
	public async Task UpdateRolesFromName([Name("name | tag")][Remainder] string name)
	{
		(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
		if (success && user is not null)
			await CheckUserAndUpdateRoles(user);
	}

	[Command("id")]
	[Remarks("pb id 743431502842298368")]
	[Priority(3)]
	public async Task UpdateRolesFromDiscordId([Name("Discord ID")] ulong discordId)
	{
		(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
		if (success && user is not null)
			await CheckUserAndUpdateRoles(user);
	}

	private async Task CheckUserAndUpdateRoles(SocketGuildUser user)
	{
		(bool isError, string? message) = _userService.IsValid(user, user.Id == Context.User.Id);
		if (isError && message is not null)
		{
			await InlineReplyAsync(message);
			return;
		}

		UpdateRolesResponse response = await _updateRolesHelper.UpdateUserRoles(user);

		if (!response.Success)
			await InlineReplyAsync("No updates were needed.");
		else
			await ReplyAsync(embed: EmbedHelper.UpdateRoles(response), allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
	}
}
