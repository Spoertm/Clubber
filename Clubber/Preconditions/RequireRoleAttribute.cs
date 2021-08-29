using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Preconditions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
	public class RequireRoleAttribute : PreconditionAttribute
	{
		private readonly ulong _requiredRoleId;

		public RequireRoleAttribute(ulong requiredRoleId) => _requiredRoleId = requiredRoleId;

		public override string ErrorMessage { get; set; } = string.Empty;

		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			if (context.User is not IGuildUser guildUser)
				return Task.FromResult(PreconditionResult.FromError("Command has to be executed in a guild channel."));

			if (guildUser.GuildPermissions.Administrator || guildUser.RoleIds.Contains(_requiredRoleId))
				return Task.FromResult(PreconditionResult.FromSuccess());

			IRole? requiredRole = context.Guild.GetRole(_requiredRoleId);
			return Task.FromResult(PreconditionResult.FromError(ErrorMessage.Length == 0 ? $"Only users with {requiredRole.Name} role can use this command." : ErrorMessage));
		}
	}
}
