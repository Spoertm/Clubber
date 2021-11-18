using Clubber.Configuration;
using Clubber.Helpers;
using Clubber.Models.Responses;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public class WelcomeMessage
	{
		private readonly IConfig _config;
		private readonly IDatabaseHelper _databaseHelper;
		private readonly UpdateRolesHelper _updateRolesHelper;

		public WelcomeMessage(IConfig config, IDatabaseHelper databaseHelper, UpdateRolesHelper updateRolesHelper, DiscordSocketClient client)
		{
			_config = config;
			_databaseHelper = databaseHelper;
			_updateRolesHelper = updateRolesHelper;
			client.UserJoined += OnUserJoined;
		}

		private async Task OnUserJoined(SocketGuildUser joiningUser)
		{
			if (joiningUser.Guild.Id != _config.DdPalsId || joiningUser.IsBot)
				return;

			// User is registered
			if (_databaseHelper.GetDdUserByDiscordId(joiningUser.Id) is not null)
				await UpdateRolesForRegisteredUser(joiningUser);
			else
				await PostWelcomeMessageAndGiveUnregRole(joiningUser);
		}

		private async Task UpdateRolesForRegisteredUser(SocketGuildUser joiningUser)
		{
			UpdateRolesResponse response = await _updateRolesHelper.UpdateUserRoles(joiningUser);
			if (!response.Success)
				return;

			if (joiningUser.Guild.GetChannel(_config.CronUpdateChannelId) is SocketTextChannel logsChannel)
				await logsChannel.SendMessageAsync(null, false, EmbedHelper.UpdateRoles(response));
		}

		private async Task PostWelcomeMessageAndGiveUnregRole(SocketGuildUser joiningUser)
		{
			await joiningUser.AddRoleAsync(_config.UnregisteredRoleId);
			if (joiningUser.Guild.GetChannel(_config.DdPalsRegisterChannelId) is SocketTextChannel registerChannel)
				await registerChannel.SendMessageAsync($"Welcome {joiningUser.Mention}! Please refer to the first message in this channel for info about registration.");
		}
	}
}
