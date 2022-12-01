using Clubber.Domain.Helpers;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Domain.Modules;

[Name("Database")]
[Group("register")]
[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
[RequireContext(ContextType.Guild)]
public class RegisterUser : ExtendedModulebase<SocketCommandContext>
{
	private readonly IDatabaseHelper _databaseHelper;
	private readonly UserService _userService;

	public RegisterUser(IDatabaseHelper databaseHelper, UserService userService)
	{
		_databaseHelper = databaseHelper;
		_userService = userService;
	}

	[Command]
	[Remarks("register 118832 clubber\nregister 118832 <@743431502842298368>")]
	[Priority(1)]
	public async Task RegisterByName([Name("leaderboard ID")] uint lbId, [Name("name | tag")][Remainder] string name)
	{
		(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
		if (success && user is not null)
			await CheckUserAndRegister(lbId, user);
	}

	[Command("id")]
	[Remarks("register id 118832 743431502842298368")]
	[Priority(2)]
	public async Task RegisterByDiscordId([Name("leaderboard ID")] uint lbId, [Name("Discord ID")] ulong discordId)
	{
		(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
		if (success && user is not null)
			await CheckUserAndRegister(lbId, user);
	}

	private async Task CheckUserAndRegister(uint lbId, SocketGuildUser user)
	{
		(bool isError, string? message) = _userService.IsValidForRegistration(user, user.Id == Context.User.Id);
		if (isError && message is not null)
		{
			await InlineReplyAsync(message);
			return;
		}

		(bool success, string registerResponseMessage) = await _databaseHelper.RegisterUser(lbId, user);
		if (success)
		{
			const ulong newPalRoleId = 728663492424499200;
			const ulong pendingPbRoleId = 994354086646399066;
			await user.RemoveRoleAsync(newPalRoleId);
			await user.AddRoleAsync(pendingPbRoleId);
			await InlineReplyAsync("✅ Successfully registered.\n\nDo `+pb` anywhere to get assigned a role.");
		}
		else
		{
			await InlineReplyAsync($"Failed to execute command: {registerResponseMessage}");
		}
	}
}
