using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;

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
		Result result = await _userService.IsValid(user, user.Id == Context.User.Id);
		if (result.IsFailure)
		{
			await InlineReplyAsync(result.ErrorMsg);
			return;
		}

		UpdateRolesResponse response = await _updateRolesHelper.UpdateUserRoles(user);
		if (response is UpdateRolesResponse.Full fullResponse)
		{
			await ReplyAsync(embed: EmbedHelper.UpdateRoles(fullResponse), allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
		}
		else if (response is UpdateRolesResponse.Partial partialResponse)
		{
			string msg = "No updates were needed.";
			if (partialResponse.SecondsAwayFromNextRole == 0)
			{
				msg += "\n\nYou already have the highest role in the server!";
			}
			else
			{
				msg += $"\n\nYou're **{partialResponse.SecondsAwayFromNextRole:0.0000}s** away from the next role: {MentionUtils.MentionRole(partialResponse.NextRoleId)}";
			}

			await InlineReplyAsync(msg);
		}
		else
		{
			throw new UnreachableException($"{nameof(UpdateRolesResponse)} isn't supposed to have a third state.");
		}
	}
}
