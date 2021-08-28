using Clubber.Configuration;
using Clubber.Helpers;
using Clubber.Models.Responses;
using Discord;
using System.Linq;

namespace Clubber.Services
{
	public class UserService
	{
		private readonly IDatabaseHelper _databaseHelper;

		public UserService(IDatabaseHelper databaseHelper)
		{
			_databaseHelper = databaseHelper;
		}

		public UserValidationResponse IsValidForRegistration(IGuildUser guildUser, bool userUsedCommandForThemselves)
		{
			(bool responseIsError, string? responseMessage) = IsBotOrCheater(guildUser, userUsedCommandForThemselves);
			if (responseIsError)
				return new(IsError: true, Message: responseMessage);

			if (_databaseHelper.GetDdUserByDiscordId(guildUser.Id) is not null)
				return new(IsError: true, Message: $"User `{guildUser.Username}` is already registered.");

			return new(IsError: false, Message: null);
		}

		public UserValidationResponse IsValid(IGuildUser guildUser, bool userUsedCommandForThemselves)
		{
			(bool reponseIsError, string? responseMessage) = IsBotOrCheater(guildUser, userUsedCommandForThemselves);
			if (reponseIsError)
				return new(IsError: true, Message: responseMessage);

			if (_databaseHelper.GetDdUserByDiscordId(guildUser.Id) is not null)
				return new(IsError: false, Message: null);

			if (guildUser.GuildPermissions.ManageRoles)
				return new(IsError: true, $"`{guildUser.Username}` is not registered.");

			string message = userUsedCommandForThemselves
				? $"You're not registered, {guildUser.Username}. Only a <@&{Config.RoleAssignerRoleId}> can register you.\nPlease refer to the message in <#{Config.RegisterChannelId}> for more info."
				: $"`{guildUser.Username}` is not registered. Only a <@&{Config.RoleAssignerRoleId}> can register them.\nPlease refer to the message in <#{Config.RegisterChannelId}> for more info.";

			return new(IsError: true, Message: message);
		}

		private static UserValidationResponse IsBotOrCheater(IGuildUser guildUser, bool userUsedCommandForThemselves)
		{
			if (guildUser.IsBot)
				return new(IsError: true, Message: $"{guildUser.Mention} is a bot. It can't be registered as a DD player.");

			if (guildUser.RoleIds.All(rId => rId != Config.CheaterRoleId))
				return new(IsError: false, Message: null);

			string message = userUsedCommandForThemselves
				? $"{guildUser.Username}, you can't register because you've cheated."
				: $"{guildUser.Username} can't be registered because they've cheated.";

			return new(IsError: true, Message: message);
		}
	}
}
