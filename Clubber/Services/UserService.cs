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
		private readonly IConfig _config;

		public UserService(IConfig config, IDatabaseHelper databaseHelper)
		{
			_config = config;
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

			bool userHasUnregRole = guildUser.RoleIds.Contains(_config.UnregisteredRoleId);
			string message = userUsedCommandForThemselves
				? $"You're not registered, {guildUser.Username}. Only a <@&{_config.RoleAssignerRoleId}> can register you."
				: $"`{guildUser.Username}` is not registered. Only a <@&{_config.RoleAssignerRoleId}> can register them.";

			if (userHasUnregRole)
				message += $"\nPlease refer to the message in <#{_config.RegisterChannelId}> for more info.";

			return new(IsError: true, Message: message);
		}

		private UserValidationResponse IsBotOrCheater(IGuildUser guildUser, bool userUsedCommandForThemselves)
		{
			if (guildUser.IsBot)
				return new(IsError: true, Message: $"{guildUser.Mention} is a bot. It can't be registered as a DD player.");

			if (guildUser.RoleIds.All(rId => rId != _config.CheaterRoleId))
				return new(IsError: false, Message: null);

			string message = userUsedCommandForThemselves
				? $"{guildUser.Username}, you can't register because you've cheated."
				: $"{guildUser.Username} can't be registered because they've cheated.";

			return new(IsError: true, Message: message);
		}
	}
}
