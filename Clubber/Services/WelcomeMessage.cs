using Clubber.Helpers;
using Clubber.Models;
using Clubber.Models.Responses;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Services
{
	[RequireContext(ContextType.Guild)]
	public class WelcomeMessage
	{
		private readonly DatabaseHelper _databaseHelper;
		private readonly UpdateRolesHelper _updateRolesHelper;

		public WelcomeMessage(DatabaseHelper databaseHelper, UpdateRolesHelper updateRolesHelper, DiscordSocketClient client)
		{
			_databaseHelper = databaseHelper;
			_updateRolesHelper = updateRolesHelper;
			client.UserJoined += OnUserJoined;
		}

		private async Task OnUserJoined(SocketGuildUser joiningUser)
		{
			if (joiningUser.Guild.Id != Constants.DdPalsId)
				return;

			// User is registered
			if (_databaseHelper.GetDdUserByDiscordId(joiningUser.Id) is not null)
				await UpdateRolesForRegisteredUser(joiningUser);
			else
				await PostWelcomeMessage(joiningUser);
		}

		private async Task UpdateRolesForRegisteredUser(SocketGuildUser joiningUser)
		{
			UpdateRolesResponse response = await _updateRolesHelper.UpdateUserRoles(joiningUser);
			if (!response.Success)
				return;

			if (joiningUser.Guild.GetChannel(Constants.DdPalsRegisterChannelId) is SocketTextChannel registerChannel)
				await registerChannel.SendMessageAsync(null, false, EmbedHelper.UpdateRoles(response));
		}

		private async Task PostWelcomeMessage(SocketGuildUser joiningUser)
		{
			string message =
				@$"Welcome {joiningUser.Mention}!

<@743431502842298368> is a bot that takes care of players' score roles so when you get a new personal best, it updates your score role. E.g. <@&805789474575482921> to <@&805789480099381308>. For it to do this you need be registered.

If you want to be registered post your **in-game** name or ID; otherwise post 'no score'.
A moderator will then soon register you.

Use this website if you don't know your ID: https://devildaggers.info/Leaderboard
Simply hover over your rank and it should appear.";

			if (joiningUser.Guild.GetChannel(Constants.DdPalsRegisterChannelId) is SocketTextChannel registerChannel)
				await registerChannel.SendMessageAsync(message);
		}
	}
}
