using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Exceptions;
using Discord;
using Microsoft.Extensions.Configuration;

namespace Clubber.Domain.Services;

public class UserService
{
	private readonly IConfiguration _config;
	private readonly IDatabaseHelper _databaseHelper;

	public UserService(IConfiguration config, IDatabaseHelper databaseHelper)
	{
		_config = config;
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
			return Result.Failure($"User `{guildUser.Username}` is already registered.");
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
			return Result.Failure($"`{guildUser.Username}` is not registered.");
		}

		ulong unregRoleId = _config.GetValue<ulong>("UnregisteredRoleId");
		bool userHasUnregRole = guildUser.RoleIds.Contains(unregRoleId);

		string roleAssignerRoleId = _config["RoleAssignerRoleId"] ?? throw new ConfigurationMissingException("RoleAssignerRoleId");
		string message = userUsedCommandForThemselves
			? $"You're not registered, {guildUser.Username}. Only a <@&{roleAssignerRoleId}> can register you."
			: $"`{guildUser.Username}` is not registered. Only a <@&{roleAssignerRoleId}> can register them.";

		string registerChannelId = _config["RegisterChannelId"] ?? throw new ConfigurationMissingException("RegisterChannelId");
		if (userHasUnregRole)
		{
			message += $"\nPlease refer to the first message in <#{registerChannelId}> for more info.";
		}

		return Result.Failure(message);
	}

	public Result IsBotOrCheater(IGuildUser guildUser, bool userUsedCommandForThemselves)
	{
		if (guildUser.IsBot)
		{
			return Result.Failure($"{guildUser.Mention} is a bot. It can't be registered as a DD player.");
		}

		ulong cheaterRoleId = _config.GetValue<ulong>("CheaterRoleId");
		if (guildUser.RoleIds.All(rId => rId != cheaterRoleId))
		{
			return Result.Success();
		}

		string message = userUsedCommandForThemselves
			? $"{guildUser.Username}, you can't register because you've cheated."
			: $"{guildUser.Username} can't be registered because they've cheated.";

		return Result.Failure(message);
	}
}
