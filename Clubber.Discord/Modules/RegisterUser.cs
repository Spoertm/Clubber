﻿using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Clubber.Discord.Modules;

[Name("Database")]
[Group("register")]
[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
[RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "Only users with higher permissions can use this command. Ask a `Role assigner` or a Moderator/Admin to help you.")]
[RequireContext(ContextType.Guild)]
public class RegisterUser : ExtendedModulebase<SocketCommandContext>
{
	private readonly IOptionsMonitor<BotConfig> _botConfig;
	private readonly IDatabaseHelper _databaseHelper;
	private readonly UserService _userService;

	public RegisterUser(IOptionsMonitor<BotConfig> botConfig, IDatabaseHelper databaseHelper, UserService userService)
	{
		_botConfig = botConfig;
		_databaseHelper = databaseHelper;
		_userService = userService;
	}

	[Command]
	[Remarks("register 118832 clubber\nregister 118832 <@743431502842298368>")]
	[Priority(1)]
	public async Task RegisterByName([Name("leaderboard ID")] uint lbId, [Name("name | tag")][Remainder] string name)
	{
		Result<SocketGuildUser> result = await FoundOneUserFromName(name);
		if (result.IsSuccess)
			await CheckUserAndRegister(lbId, result.Value);
	}

	[Command("id")]
	[Remarks("register id 118832 743431502842298368")]
	[Priority(2)]
	public async Task RegisterByDiscordId([Name("leaderboard ID")] uint lbId, [Name("Discord ID")] ulong discordId)
	{
		Result<SocketGuildUser> result = await FoundUserFromDiscordId(discordId);
		if (result.IsSuccess)
			await CheckUserAndRegister(lbId, result.Value);
	}

	private async Task CheckUserAndRegister(uint lbId, SocketGuildUser user)
	{
		Result result = await _userService.IsValidForRegistration(user, user.Id == Context.User.Id);
		if (result.IsFailure)
		{
			await InlineReplyAsync(result.ErrorMsg);
			return;
		}

		Result registrationResult = await _databaseHelper.RegisterUser(lbId, user.Id);
		if (registrationResult.IsSuccess)
		{
			await user.RemoveRoleAsync(_botConfig.CurrentValue.NewPalRoleId);
			await user.AddRoleAsync(_botConfig.CurrentValue.PendingPbRoleId);
			await InlineReplyAsync("✅ Successfully registered.\n\nDo `+pb` anywhere to get assigned a role.");
		}
		else
		{
			await InlineReplyAsync($"Failed to execute command: {registrationResult.ErrorMsg}");
		}
	}
}
