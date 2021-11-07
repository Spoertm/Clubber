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
			string message =
				@$"Welcome {joiningUser.Mention}!

<@743431502842298368> is a bot that takes care of players' score roles so when you get a new personal best, it updates your score role. E.g. <@&805789474575482921> to <@&805789480099381308>. For it to do this you need be registered.

If you want to be registered post your **in-game** name or ID; otherwise post 'no score'.
A moderator will then soon register you.

Use this website if you don't know your ID: https://devildaggers.info/Leaderboard
Simply hover over your rank and it should appear.";

			if (joiningUser.Guild.GetChannel(_config.DdPalsRegisterChannelId) is SocketTextChannel registerChannel)
				await registerChannel.SendMessageAsync(message);
		}
	}
}
