﻿using Discord;
using Discord.Commands;

namespace Clubber.Discord.Models;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireAdminOrRoleAttribute : PreconditionAttribute
{
	private readonly ulong _requiredRoleId;

	public RequireAdminOrRoleAttribute(ulong requiredRoleId) => _requiredRoleId = requiredRoleId;

	public override string? ErrorMessage { get; set; }

	public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
	{
		if (context.User is not IGuildUser guildUser)
		{
			return Task.FromResult(PreconditionResult.FromError("Command can only be used in a guild."));
		}

		if (guildUser.GuildPermissions.Administrator || guildUser.RoleIds.Contains(_requiredRoleId))
		{
			return Task.FromResult(PreconditionResult.FromSuccess());
		}

		IRole? requiredRole = context.Guild.GetRole(_requiredRoleId);
		string roleName = requiredRole is null ? MentionUtils.MentionRole(_requiredRoleId) : requiredRole.Name;
		return Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? $"Only users with `{roleName}` role can use this command."));
	}
}
