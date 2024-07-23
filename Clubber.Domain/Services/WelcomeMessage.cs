using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Discord;
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
			await UpdateRolesForRegisteredUser(joiningUser);
		}
		else
		{
			await joiningUser.AddRoleAsync(unregRoleId);
		}
	}

	private async Task UpdateRolesForRegisteredUser(IGuildUser joiningUser)
	{
		await using AsyncServiceScope scope = _services.CreateAsyncScope();
		UpdateRolesHelper updateRolesHelper = scope.ServiceProvider.GetRequiredService<UpdateRolesHelper>();
		UpdateRolesResponse response = await updateRolesHelper.UpdateUserRoles(joiningUser);

		if (response is UpdateRolesResponse.Full fullResponse)
		{
			ulong logChannelId = _config.GetValue<ulong>("DailyUpdateLoggingChannelId");
			if (await joiningUser.Guild.GetChannelAsync(logChannelId) is ITextChannel logsChannel)
			{
				await logsChannel.SendMessageAsync(embeds: [EmbedHelper.UpdateRoles(fullResponse)]);
			}
		}
	}
}
