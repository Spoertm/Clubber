using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Clubber.Discord;

public class UserJoinHandler
{
	private readonly IServiceScopeFactory _services;
	private readonly AppConfig _config;

	public UserJoinHandler(
		IServiceScopeFactory services,
		IOptions<AppConfig> config,
		DiscordSocketClient client)
	{
		_services = services;
		_config = config.Value;
		client.UserJoined += OnUserJoined;
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
		UpdateRolesHelper updateRolesHelper = scope.ServiceProvider.GetRequiredService<UpdateRolesHelper>();
		UpdateRolesResponse response = await updateRolesHelper.UpdateUserRoles(joiningUser);

		if (response is UpdateRolesResponse.Full fullResponse)
		{
			ulong logChannelId = _config.DailyUpdateLoggingChannelId;
			if (await joiningUser.Guild.GetChannelAsync(logChannelId) is ITextChannel logsChannel)
			{
				await logsChannel.SendMessageAsync(embeds: [EmbedHelper.UpdateRoles(fullResponse)]);
			}
		}
	}
}
