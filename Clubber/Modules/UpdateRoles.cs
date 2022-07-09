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
[Summary("Updates your DD score roles if necessary.")]
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
	public async Task UpdateRolesFromCurrentUser()
	{
		SocketGuildUser user = Context.Guild.GetUser(Context.User.Id);
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
