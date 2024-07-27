using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Discord;
using Microsoft.Extensions.Options;

namespace Clubber.Discord;

public class UserService
{
	private readonly AppConfig _config;
	private readonly IDatabaseHelper _databaseHelper;

	public UserService(IOptions<AppConfig> config, IDatabaseHelper databaseHelper)
	{
		_config = config.Value;
		_databaseHelper = databaseHelper;
	}

	public async Task<Result> IsValidForRegistration(IGuildUser guildUser, bool userUsedCommandForThemselves)
	{
		Result result = IsBotOrCheater(guildUser, userUsedCommandForThemselves);
		if (result.IsFailure)
		{
			return Result.Failure(result.ErrorMsg);
		}

		if (await _databaseHelper.GetDdUserBy(guildUser.Id) is not null)
		{
			return Result.Failure($"User `{guildUser.AvailableNameSanitized()}` is already registered.");
		}

		return Result.Success();
	}

	public async Task<Result> IsValid(IGuildUser guildUser, bool userUsedCommandForThemselves)
	{
		Result result = IsBotOrCheater(guildUser, userUsedCommandForThemselves);
		if (result.IsFailure)
		{
			return Result.Failure(result.ErrorMsg);
		}

		if (await _databaseHelper.GetDdUserBy(guildUser.Id) is not null)
		{
			return Result.Success();
		}

		if (guildUser.GuildPermissions.ManageRoles)
		{
			return Result.Failure($"`{guildUser.AvailableNameSanitized()}` is not registered.");
		}

		bool userHasUnregRole = guildUser.RoleIds.Contains(_config.UnregisteredRoleId);

		string message = userUsedCommandForThemselves
			? $"You're not registered, {guildUser.AvailableNameSanitized()}. Only a <@&{_config.RoleAssignerRoleId}> can register you."
			: $"`{guildUser.AvailableNameSanitized()}` is not registered. Only a <@&{_config.RoleAssignerRoleId}> can register them.";

		if (userHasUnregRole)
		{
			message += $"\nPlease refer to the first message in <#{_config.RegisterChannelId}> for more info.";
		}

		return Result.Failure(message);
	}

	public Result IsBotOrCheater(IGuildUser guildUser, bool userUsedCommandForThemselves)
	{
		if (guildUser.IsBot)
		{
			return Result.Failure($"{guildUser.Mention} is a bot. It can't be registered as a DD player.");
		}

		if (guildUser.RoleIds.All(rId => rId != _config.CheaterRoleId))
		{
			return Result.Success();
		}

		string message = userUsedCommandForThemselves
			? $"{guildUser.AvailableNameSanitized()}, you can't register because you've cheated."
			: $"{guildUser.AvailableNameSanitized()} can't be registered because they've cheated.";

		return Result.Failure(message);
	}
}
