using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Roles")]
	[Group("pb")]
	[Alias("updateroles")]
	[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles.")]
	[RequireContext(ContextType.Guild)]
	public class UpdateRoles : ExtendedModulebase<SocketCommandContext>
	{
		private readonly UpdateRolesHelper _updateRolesHelper;
		private readonly UserService _userService;

		public UpdateRoles(UpdateRolesHelper updateRolesHelper, UserService userService)
		{
			_updateRolesHelper = updateRolesHelper;
			_userService = userService;
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
			UserValidationResponse userIsValidResponse = _userService.IsValid(user, Context);
			if (userIsValidResponse.IsError)
			{
				await InlineReplyAsync(userIsValidResponse.Message!);
				return;
			}

			UpdateRolesResponse response = await _updateRolesHelper.UpdateUserRoles(user);

			if (!response.Success)
				await InlineReplyAsync("No updates were needed.");
			else
				await ReplyAsync(null, false, EmbedHelper.UpdateRoles(response), null, AllowedMentions.None, new(Context.Message.Id));
		}
	}
}
