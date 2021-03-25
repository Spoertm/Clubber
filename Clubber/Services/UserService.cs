using Clubber.Helpers;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;

namespace Clubber.Services
{
	public class UserService
	{
		private readonly DatabaseHelper _databaseHelper;

		public UserService(DatabaseHelper databaseHelper)
		{
			_databaseHelper = databaseHelper;
		}

		public UserValidationResponse IsValidForRegistration(SocketGuildUser guildUser, ICommandContext context)
		{
			UserValidationResponse isBotOrCheaterResponse = IsBotOrCheater(guildUser, context);
			if (isBotOrCheaterResponse.IsError)
				return new(IsError: true, Message: isBotOrCheaterResponse.Message);

			if (_databaseHelper.GetDdUserByDiscordId(guildUser.Id) is not null)
				return new(IsError: true, Message: $"User `{guildUser.Username}` is already registered.");

			return new(IsError: false, Message: null);
		}

		public UserValidationResponse IsValid(SocketGuildUser guildUser, ICommandContext context)
		{
			UserValidationResponse isBotOrCheaterResponse = IsBotOrCheater(guildUser, context);
			if (isBotOrCheaterResponse.IsError)
				return new(IsError: true, Message: isBotOrCheaterResponse.Message);

			if (_databaseHelper.GetDdUserByDiscordId(guildUser.Id) is null)
			{
				if (guildUser.GuildPermissions.ManageRoles)
					return new(IsError: true, $"`{guildUser.Username}` is not registered.");

				string message = guildUser.Id == context.User.Id
					? $"You're not registered, {guildUser.Username}. Only a <@&{Constants.RoleAssignerRoleId}> can register you, and one should be here soon.\nPlease refer to the message in <#{Constants.RegisterChannelId}> for more info."
					: $"`{guildUser.Username}` is not registered. Only a <@&{Constants.RoleAssignerRoleId}> can register them, and one should be here soon.\nPlease refer to the message in <#{Constants.RegisterChannelId}> for more info.";

				return new(IsError: true, Message: message);
			}

			return new(IsError: false, Message: null);
		}

		private static UserValidationResponse IsBotOrCheater(SocketGuildUser guildUser, ICommandContext context)
		{
			if (guildUser.IsBot)
				return new(IsError: true, Message: $"{guildUser.Mention} is a bot. It can't be registered as a DD player.");

			if (guildUser.Roles.Any(r => r.Id == Constants.CheaterRoleId))
			{
				string message = guildUser.Id == context.User.Id
				? $"{guildUser.Username}, you can't register because you've cheated."
				: $"{guildUser.Username} can't be registered because they've cheated.";

				return new(IsError: true, Message: message);
			}

			return new(IsError: false, Message: null);
		}
	}
}
