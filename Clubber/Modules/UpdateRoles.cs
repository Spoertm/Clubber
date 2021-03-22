using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Roles")]
	[Group("pb")]
	[Alias("updateroles")]
	[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles.")]
	public class UpdateRoles : AbstractModule<SocketCommandContext>
	{
		private readonly UpdateRolesHelper _updateRolesHelper;

		public UpdateRoles(DatabaseHelper databaseHelper, UpdateRolesHelper updateRolesHelper)
			: base(databaseHelper)
		{
			_updateRolesHelper = updateRolesHelper;
		}

		[Command]
		[Remarks("pb")]
		[Priority(1)]
		public async Task UpdateRolesFromCurrentUser() => await CheckUserAndUpdateRoles(Context.Guild.GetUser(Context.User.Id));

		[Command]
		[Remarks("pb clubber\npb <@743431502842298368>")]
		[Priority(2)]
		public async Task UpdateRolesFromName([Name("name | tag")][Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user is not null)
				await CheckUserAndUpdateRoles(user);
		}

		[Command("id")]
		[Remarks("pb id 743431502842298368")]
		[Priority(3)]
		public async Task UpdateRolesFromDiscordId([Name("Discord ID")] ulong discordId)
		{
			(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
			if (success && user is not null)
				await CheckUserAndUpdateRoles(user);
		}

		private async Task CheckUserAndUpdateRoles(SocketGuildUser user)
		{
			if (!await UserIsClean(user, checkIfCheater: true, checkIfBot: true, checkIfAlreadyRegistered: false, checkIfNotRegistered: true))
				return;

			UpdateRolesResponse response = await _updateRolesHelper.UpdateUserRoles(user);

			if (!response.Success)
				await InlineReplyAsync("No updates were needed.");
			else
				await ReplyAsync(null, false, EmbedHelper.UpdateRoles(response), null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}

		[Command("database")]
		[Priority(4)]
		[RequireOwner]
		public async Task UpdateDatabase()
		{
			Stopwatch stopwatch = new();
			stopwatch.Start();

			const string checkingString = "Checking for role updates...";
			IUserMessage msg = await ReplyAsync(checkingString);

			DatabaseUpdateResponse response = await _updateRolesHelper.UpdateRolesAndDb(Context.Guild.Users);
			await msg.ModifyAsync(m => m.Content = $"{checkingString}\n{response.Message}");

			for (int i = 0; i < response.RoleUpdateEmbeds.Length; i++)
				await ReplyAsync(null, false, response.RoleUpdateEmbeds[i]);
		}
	}
}
