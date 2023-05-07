using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clubber.Domain.Services;

public class WelcomeMessage
{
	private readonly IServiceScopeFactory _services;
	private readonly IConfiguration _config;

	public WelcomeMessage(
		IServiceScopeFactory services,
		IConfiguration config,
		DiscordSocketClient client)
	{
		_services = services;
		_config = config;
		client.UserJoined += OnUserJoined;
	}

	private async Task OnUserJoined(SocketGuildUser joiningUser)
	{
		ulong ddpalsId = _config.GetValue<ulong>("DdPalsId");
		if (joiningUser.Guild.Id != ddpalsId || joiningUser.IsBot)
			return;

		// User is registered
		ulong unregRoleId = _config.GetValue<ulong>("UnregisteredRoleId");

		await using AsyncServiceScope scope = _services.CreateAsyncScope();
		IDatabaseHelper dbHelper = scope.ServiceProvider.GetRequiredService<IDatabaseHelper>();

		if (await dbHelper.GetDdUserBy(joiningUser.Id) is not null)
		{
			await UpdateRolesForRegisteredUser(scope, joiningUser);
		}
		else
		{
			await joiningUser.AddRoleAsync(unregRoleId);
		}
	}

	private async Task UpdateRolesForRegisteredUser(AsyncServiceScope scope, SocketGuildUser joiningUser)
	{
		UpdateRolesHelper updateRolesHelper = scope.ServiceProvider.GetRequiredService<UpdateRolesHelper>();
		UpdateRolesResponse response = await updateRolesHelper.UpdateUserRoles(joiningUser);

		if (!response.Success)
		{
			return;
		}

		ulong logChannelId = _config.GetValue<ulong>("DailyUpdateLoggingChannelId");
		if (joiningUser.Guild.GetChannel(logChannelId) is SocketTextChannel logsChannel)
		{
			await logsChannel.SendMessageAsync(null, false, EmbedHelper.UpdateRoles(response));
		}
	}
}
