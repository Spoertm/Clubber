using Clubber.Helpers;
using Clubber.Models.Responses;
using Discord.WebSocket;

namespace Clubber.Services;

public class WelcomeMessage
{
	private readonly IConfiguration _config;
	private readonly IDatabaseHelper _databaseHelper;
	private readonly UpdateRolesHelper _updateRolesHelper;

	public WelcomeMessage(
		IConfiguration config,
		IDatabaseHelper databaseHelper,
		UpdateRolesHelper updateRolesHelper,
		DiscordSocketClient client)
	{
		_config = config;
		_databaseHelper = databaseHelper;
		_updateRolesHelper = updateRolesHelper;
		client.UserJoined += OnUserJoined;
	}

	private async Task OnUserJoined(SocketGuildUser joiningUser)
	{
		ulong ddpalsId = _config.GetValue<ulong>("DdPalsId");
		if (joiningUser.Guild.Id != ddpalsId || joiningUser.IsBot)
			return;

		// User is registered
		ulong unregRoleId = _config.GetValue<ulong>("UnregisteredRoleId");
		if (_databaseHelper.GetDdUserBy(joiningUser.Id) is not null)
			await UpdateRolesForRegisteredUser(joiningUser);
		else
			await joiningUser.AddRoleAsync(unregRoleId);
	}

	private async Task UpdateRolesForRegisteredUser(SocketGuildUser joiningUser)
	{
		UpdateRolesResponse response = await _updateRolesHelper.UpdateUserRoles(joiningUser);
		if (!response.Success)
			return;

		ulong dailyUpdateChannelId = _config.GetValue<ulong>("DailyUpdateChannelId");
		if (joiningUser.Guild.GetChannel(dailyUpdateChannelId) is SocketTextChannel logsChannel)
			await logsChannel.SendMessageAsync(null, false, EmbedHelper.UpdateRoles(response));
	}
}
