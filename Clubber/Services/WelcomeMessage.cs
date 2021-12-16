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
				await joiningUser.AddRoleAsync(_config.UnregisteredRoleId);
		}

		private async Task UpdateRolesForRegisteredUser(SocketGuildUser joiningUser)
		{
			UpdateRolesResponse response = await _updateRolesHelper.UpdateUserRoles(joiningUser);
			if (!response.Success)
				return;

			if (joiningUser.Guild.GetChannel(_config.CronUpdateChannelId) is SocketTextChannel logsChannel)
				await logsChannel.SendMessageAsync(embed: EmbedHelper.UpdateRoles(response));
		}
	}
}
