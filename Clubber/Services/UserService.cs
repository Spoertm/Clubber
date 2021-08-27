using Clubber.Configuration;
using Clubber.Helpers;
using Clubber.Models.Responses;
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
			(bool responseIsError, string? responseMessage) = IsBotOrCheater(guildUser, context);
			if (responseIsError)
				return new(IsError: true, Message: responseMessage);

			if (_databaseHelper.GetDdUserByDiscordId(guildUser.Id) is not null)
				return new(IsError: true, Message: $"User `{guildUser.Username}` is already registered.");

			return new(IsError: false, Message: null);
		}

		public UserValidationResponse IsValid(SocketGuildUser guildUser, ICommandContext context)
		{
			(bool reponseIsError, string? responseMessage) = IsBotOrCheater(guildUser, context);
			if (reponseIsError)
				return new(IsError: true, Message: responseMessage);

			if (_databaseHelper.GetDdUserByDiscordId(guildUser.Id) is not null)
				return new(IsError: false, Message: null);

			if (guildUser.GuildPermissions.ManageRoles)
				return new(IsError: true, $"`{guildUser.Username}` is not registered.");

			string message = guildUser.Id == context.User.Id
				? $"You're not registered, {guildUser.Username}. Only a <@&{Config.RoleAssignerRoleId}> can register you.\nPlease refer to the message in <#{Config.RegisterChannelId}> for more info."
				: $"`{guildUser.Username}` is not registered. Only a <@&{Config.RoleAssignerRoleId}> can register them.\nPlease refer to the message in <#{Config.RegisterChannelId}> for more info.";

			return new(IsError: true, Message: message);
		}

		private static UserValidationResponse IsBotOrCheater(SocketGuildUser guildUser, ICommandContext context)
		{
			if (guildUser.IsBot)
				return new(IsError: true, Message: $"{guildUser.Mention} is a bot. It can't be registered as a DD player.");

			if (guildUser.Roles.All(r => r.Id != Config.CheaterRoleId))
				return new(IsError: false, Message: null);

			string message = guildUser.Id == context.User.Id
				? $"{guildUser.Username}, you can't register because you've cheated."
				: $"{guildUser.Username} can't be registered because they've cheated.";

			return new(IsError: true, Message: message);
		}
	}
}
