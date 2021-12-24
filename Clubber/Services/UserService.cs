using Clubber.Helpers;
using Clubber.Models.Responses;
using Discord;

namespace Clubber.Services
{
	public class UserService
	{
		private readonly IDatabaseHelper _databaseHelper;

		public UserService(IDatabaseHelper databaseHelper) => _databaseHelper = databaseHelper;

		public UserValidationResponse IsValidForRegistration(IGuildUser guildUser, bool userUsedCommandForThemselves)
		{
			(bool responseIsError, string? responseMessage) = IsBotOrCheater(guildUser, userUsedCommandForThemselves);
			if (responseIsError)
				return new(IsError: true, Message: responseMessage);

			if (_databaseHelper.GetDdUserBy(ddu => ddu.DiscordId, guildUser.Id) is not null)
				return new(IsError: true, Message: $"User `{guildUser.Username}` is already registered.");

			return new(IsError: false, Message: null);
		}

		public UserValidationResponse IsValid(IGuildUser guildUser, bool userUsedCommandForThemselves)
		{
			(bool reponseIsError, string? responseMessage) = IsBotOrCheater(guildUser, userUsedCommandForThemselves);
			if (reponseIsError)
				return new(IsError: true, Message: responseMessage);

			if (_databaseHelper.GetDdUserBy(ddu => ddu.DiscordId, guildUser.Id) is not null)
				return new(IsError: false, Message: null);

			if (guildUser.GuildPermissions.ManageRoles)
				return new(IsError: true, $"`{guildUser.Username}` is not registered.");

			ulong unregRoleId = ulong.Parse(Environment.GetEnvironmentVariable("UnregisteredRoleId")!);
			bool userHasUnregRole = guildUser.RoleIds.Contains(unregRoleId);

			string roleAssignerRoleId = Environment.GetEnvironmentVariable("RoleAssignerRoleId")!;
			string message = userUsedCommandForThemselves
				? $"You're not registered, {guildUser.Username}. Only a <@&{roleAssignerRoleId}> can register you."
				: $"`{guildUser.Username}` is not registered. Only a <@&{roleAssignerRoleId}> can register them.";

			string registerChannelId = Environment.GetEnvironmentVariable("RegisterChannelId")!;
			if (userHasUnregRole)
				message += $"\nPlease refer to the first message in <#{registerChannelId}> for more info.";

			return new(IsError: true, Message: message);
		}

		private UserValidationResponse IsBotOrCheater(IGuildUser guildUser, bool userUsedCommandForThemselves)
		{
			if (guildUser.IsBot)
				return new(IsError: true, Message: $"{guildUser.Mention} is a bot. It can't be registered as a DD player.");

			ulong cheaterRoleId = ulong.Parse(Environment.GetEnvironmentVariable("CheaterRoleId")!);
			if (guildUser.RoleIds.All(rId => rId != cheaterRoleId))
				return new(IsError: false, Message: null);

			string message = userUsedCommandForThemselves
				? $"{guildUser.Username}, you can't register because you've cheated."
				: $"{guildUser.Username} can't be registered because they've cheated.";

			return new(IsError: true, Message: message);
		}
	}
}
