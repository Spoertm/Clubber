using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Domain.Modules;

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
		{
			string msg = "No updates were needed.";

			if (response is { SecondsAwayFromNextRole: { }, NextRoleId: { } })
			{
				msg += $"\n\nYou're **{response.SecondsAwayFromNextRole:0.0000}s** away from the next role: {MentionUtils.MentionRole(response.NextRoleId.Value)}";
			}

			await InlineReplyAsync(msg);
		}
		else
		{
			await ReplyAsync(embed: EmbedHelper.UpdateRoles(response), allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
		}
	}
}
