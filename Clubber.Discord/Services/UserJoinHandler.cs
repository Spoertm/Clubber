using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Clubber.Discord.Services;

public class UserJoinHandler
{
	private readonly IServiceScopeFactory _services;
	private readonly AppConfig _config;

	public UserJoinHandler(
		IServiceScopeFactory services,
		IOptions<AppConfig> config,
		ClubberDiscordClient discordClient)
	{
		_services = services;
		_config = config.Value;

		discordClient.UserJoined += OnUserJoined;
	}

	private async Task OnUserJoined(SocketGuildUser joiningUser)
	{
		if (joiningUser.Guild.Id != _config.DdPalsId || joiningUser.IsBot)
			return;

		// User is registered
		await using AsyncServiceScope scope = _services.CreateAsyncScope();
		IDatabaseHelper dbHelper = scope.ServiceProvider.GetRequiredService<IDatabaseHelper>();

		if (await dbHelper.FindRegisteredUser(joiningUser.Id) is not null)
		{
			await UpdateRolesForRegisteredUser(joiningUser);
		}
		else
		{
			await joiningUser.AddRoleAsync(_config.UnregisteredRoleId);
		}
	}

	private async Task UpdateRolesForRegisteredUser(IGuildUser joiningUser)
	{
		await using AsyncServiceScope scope = _services.CreateAsyncScope();
		ScoreRoleService scoreRoleService = scope.ServiceProvider.GetRequiredService<ScoreRoleService>();
		Result<RoleChangeResult> roleChangeResult = await scoreRoleService.GetRoleChange(joiningUser);

		if (roleChangeResult.IsFailure || roleChangeResult.Value is not RoleUpdate roleUpdate)
		{
			return;
		}

		if (roleUpdate.RolesToAdd.Count > 0)
		{
			await joiningUser.AddRolesAsync(roleUpdate.RolesToAdd);
		}

		if (roleUpdate.RolesToRemove.Count > 0)
		{
			await joiningUser.RemoveRolesAsync(roleUpdate.RolesToRemove);
		}

		ulong logChannelId = _config.DailyUpdateLoggingChannelId;
		if (await joiningUser.Guild.GetChannelAsync(logChannelId) is ITextChannel logsChannel)
		{
			await logsChannel.SendMessageAsync(embeds: [EmbedHelper.UpdateRoles(new UserRoleUpdate(joiningUser, roleUpdate))]);
		}
	}
}
