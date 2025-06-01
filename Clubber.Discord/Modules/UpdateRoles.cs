using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;

namespace Clubber.Discord.Modules;

[Name("Roles")]
[Group("pb")]
[Alias("updateroles")]
[Summary("Updates your DD score roles if necessary.")]
[RequireContext(ContextType.Guild)]
public class UpdateRoles : ExtendedModulebase<SocketCommandContext>
{
	private readonly ScoreRoleService _scoreRoleService;
	private readonly UserService _userService;

	public UpdateRoles(ScoreRoleService scoreRoleService, UserService userService)
	{
		_scoreRoleService = scoreRoleService;
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

		Result<RoleChangeResult> roleChangeResult = await _scoreRoleService.GetRoleChange(user);
		if (roleChangeResult.IsFailure)
		{
			await InlineReplyAsync(roleChangeResult.ErrorMsg);
			return;
		}

		if (roleChangeResult.Value is RoleUpdate roleUpdate)
		{
			if (roleUpdate.RolesToAdd.Count > 0)
			{
				await user.AddRolesAsync(roleUpdate.RolesToAdd);
			}

			if (roleUpdate.RolesToRemove.Count > 0)
			{
				await user.RemoveRolesAsync(roleUpdate.RolesToRemove);
			}

			await ReplyAsync(embed: EmbedHelper.UpdateRoles(new UserRoleUpdate(user, roleUpdate)), allowedMentions: AllowedMentions.None, messageReference: new MessageReference(Context.Message.Id));
		}
		else if (roleChangeResult.Value is RoleChangeResult.None noChangeResponse)
		{
			string msg = "No updates were needed.";
			if (noChangeResponse.SecondsAwayFromNextRole == 0)
			{
				msg += "\n\nYou already have the highest role in the server!";
			}
			else
			{
				msg += $"\n\nYou're **{noChangeResponse.SecondsAwayFromNextRole:0.0000}s** away from the next role: {MentionUtils.MentionRole(noChangeResponse.NextRoleId)}";
			}

			await InlineReplyAsync(msg);
		}
		else
		{
			throw new UnreachableException($"{nameof(RoleUpdate)} isn't supposed to have a third state.");
		}
	}
}
