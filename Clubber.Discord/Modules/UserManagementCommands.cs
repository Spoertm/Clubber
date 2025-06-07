using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;
using System.Diagnostics;

namespace Clubber.Discord.Modules;

// ===== SLASH COMMANDS =====
public sealed class UserManagementCommands(
	IDatabaseHelper databaseHelper,
	UserService userService,
	IWebService webService,
	ScoreRoleService scoreRoleService)
	: InteractionModuleBase<SocketInteractionContext>
{
	[SlashCommand("register", "Register a user with their Devil Daggers leaderboard ID")]
	[DefaultMemberPermissions(GuildPermission.ManageRoles)]
	public async Task Register(
		[Summary("user", "User to register (leave empty for yourself)")]
		SocketGuildUser user,
		[Summary("leaderboard-id", "The user's Devil Daggers leaderboard ID")]
		uint lbId)
	{
		await DeferAsync();

		try
		{
			Result result = await userService.IsValidForRegistration(user, user.Id == Context.User.Id);
			if (result.IsFailure)
			{
				await RespondAsync(result.ErrorMsg, ephemeral: true);
				return;
			}

			Result registrationResult = await databaseHelper.RegisterUser(lbId, user.Id);
			if (registrationResult.IsSuccess)
			{
				const ulong newPalRoleId = 728663492424499200;
				const ulong pendingPbRoleId = 994354086646399066;
				await user.RemoveRoleAsync(newPalRoleId);
				await user.AddRoleAsync(pendingPbRoleId);
				await FollowupAsync("✅ Successfully registered.\n\nDo `+pb` anywhere to get assigned a role.");
			}
			else
			{
				await FollowupAsync($"Failed to execute command: {registrationResult.ErrorMsg}", ephemeral: true);
			}
		}
		catch (Exception ex)
		{
			await HandleSlashCommandError(ex);
		}
	}

	[SlashCommand("unregister", "Remove a user from the database")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public async Task Unregister(
		[Summary("user", "User to unregister")]
		SocketGuildUser? user = null)
	{
		await DeferAsync();

		try
		{
			user ??= (SocketGuildUser)Context.User;

			if (await databaseHelper.RemoveUser(user.Id))
			{
				await FollowupAsync("✅ Successfully removed.", ephemeral: true);
			}
			else
			{
				await FollowupAsync("User not registered to begin with.", ephemeral: true);
			}
		}
		catch (Exception ex)
		{
			await HandleSlashCommandError(ex);
		}
	}

	[SlashCommand("link-twitch", "Link a Twitch account to your Devil Daggers profile on DDLIVE")]
	public async Task LinkTwitch(
		[Summary("twitch-username", "Your Twitch username")]
		string twitchUsername,
		[Summary("user", "User to link (leave empty for yourself)")]
		SocketGuildUser? user = null)
	{
		await DeferAsync();

		try
		{
			user ??= (SocketGuildUser)Context.User;
			bool isSelfCommand = user.Id == Context.User.Id;

			// Permission check for linking other users
			if (!isSelfCommand && !((SocketGuildUser)Context.User).GuildPermissions.ManageRoles)
			{
				await FollowupAsync("You can only link your own Twitch account, or you need ManageRoles permission to link others.", ephemeral: true);
				return;
			}

			Result result = userService.IsNotBotOrCheater(user, isSelfCommand);
			if (result.IsFailure)
			{
				await FollowupAsync(result.ErrorMsg, ephemeral: true);
				return;
			}

			if (await databaseHelper.TwitchUsernameIsRegistered(twitchUsername))
			{
				await FollowupAsync("That Twitch username is already registered.", ephemeral: true);
				return;
			}

			Result registrationResult = await databaseHelper.RegisterTwitch(user.Id, twitchUsername);
			if (registrationResult.IsSuccess)
			{
				await FollowupAsync("✅ Successfully linked Twitch.");
			}
			else
			{
				await FollowupAsync($"Failed to execute command: {registrationResult.ErrorMsg}", ephemeral: true);
			}
		}
		catch (Exception ex)
		{
			await HandleSlashCommandError(ex);
		}
	}

	[SlashCommand("unlink-twitch", "Unlink your Twitch account")]
	public async Task UnlinkTwitch(
		[Summary("user", "User to unlink (leave empty for yourself)")]
		SocketGuildUser? user = null)
	{
		try
		{
			user ??= (SocketGuildUser)Context.User;

			// Permission check for unlinking other users
			if (user.Id != Context.User.Id && !((SocketGuildUser)Context.User).GuildPermissions.ManageRoles)
			{
				await RespondAsync("You can only unlink your own Twitch account, or you need ManageRoles permission to unlink others.",
					ephemeral: true);
				return;
			}

			Result result = await databaseHelper.UnregisterTwitch(user.Id);
			if (result.IsSuccess)
			{
				await RespondAsync("✅ Successfully unlinked Twitch account.", ephemeral: true);
			}
			else
			{
				await RespondAsync($"Failed to execute command: {result.ErrorMsg}", ephemeral: true);
			}
		}
		catch (Exception ex)
		{
			await HandleSlashCommandError(ex);
		}
	}

	[SlashCommand("stats", "Get Devil Daggers statistics for a user")]
	public async Task Stats(
		[Summary("user", "User to get stats for (leave empty for yourself)")]
		SocketGuildUser? user = null,
		[Summary("full", "Show full detailed stats")]
		bool full = false)
	{
		await DeferAsync();

		try
		{
			user ??= (SocketGuildUser)Context.User;

			DdUser? ddUser = await databaseHelper.FindRegisteredUser(user.Id);

			if (ddUser is null)
			{
				Result userValidationResult = await userService.IsValid(user, user.Id == Context.User.Id);
				await FollowupAsync(userValidationResult.ErrorMsg, ephemeral: true);
				return;
			}

			uint leaderboardId = (uint)ddUser.LeaderboardId;
			Task<IReadOnlyList<EntryResponse>> playerEntryTask = webService.GetLbPlayers([leaderboardId]);
			Task<GetPlayerHistory?> playerHistoryTask = webService.GetPlayerHistory(leaderboardId);
			await Task.WhenAll(playerEntryTask, playerHistoryTask);

			EntryResponse playerEntry = (await playerEntryTask)[0];
			GetPlayerHistory? playerHistory = await playerHistoryTask;

			Embed statsEmbed;
			MessageComponent? components = null;

			if (full)
			{
				statsEmbed = EmbedHelper.FullStats(playerEntry, user, playerHistory);
			}
			else
			{
				statsEmbed = EmbedHelper.Stats(playerEntry, user, playerHistory);
				ComponentBuilder cb = new();
				cb.WithButton("Full stats", $"stats:{user.Id}:{leaderboardId}");
				components = cb.Build();
			}

			await FollowupAsync(embed: statsEmbed, components: components);
		}
		catch (Exception ex)
		{
			await HandleSlashCommandError(ex);
		}
	}

	[SlashCommand("pb", "Update your Devil Daggers score roles")]
	public async Task UpdateRoles()
	{
		await DeferAsync();

		try
		{
			SocketGuildUser user = (SocketGuildUser)Context.User;
			Result result = await userService.IsValid(user, true);
			if (result.IsFailure)
			{
				await FollowupAsync(result.ErrorMsg, ephemeral: true);
				return;
			}

			Result<RoleChangeResult> roleChangeResult = await scoreRoleService.GetRoleChange(user);
			if (roleChangeResult.IsFailure)
			{
				await FollowupAsync(roleChangeResult.ErrorMsg, ephemeral: true);
				return;
			}

			if (roleChangeResult.Value is RoleUpdate roleUpdate)
			{
				if (roleUpdate.RolesToAdd.Count > 0)
				{
					await user.AddRolesAsync(roleUpdate.RolesToAdd);
				}

				if (roleUpdate.RolesToRemove.Count > 0)
				{
					await user.RemoveRolesAsync(roleUpdate.RolesToRemove);
				}

				await FollowupAsync(embed: EmbedHelper.UpdateRoles(new UserRoleUpdate(user, roleUpdate)));
			}
			else if (roleChangeResult.Value is RoleChangeResult.None noChangeResponse)
			{
				string msg = "No updates were needed.";
				if (noChangeResponse.SecondsAwayFromNextRole == 0)
				{
					msg += "\n\nYou already have the highest role in the server!";
				}
				else
				{
					msg +=
						$"\n\nYou're **{noChangeResponse.SecondsAwayFromNextRole:0.0000}s** away from the next role: {MentionUtils.MentionRole(noChangeResponse.NextRoleId)}";
				}

				await FollowupAsync(msg);
			}
			else
			{
				throw new UnreachableException($"{nameof(RoleUpdate)} isn't supposed to have a third state.");
			}
		}
		catch (Exception ex)
		{
			await HandleSlashCommandError(ex);
		}
	}

	private async Task HandleSlashCommandError(Exception ex)
	{
		Log.Error(ex, "Slash command error");

		// Try to respond if we haven't already
		const string errorMessage = "An error occurred while processing your command.";
		if (!Context.Interaction.HasResponded)
		{
			await RespondAsync(errorMessage, ephemeral: true);
		}
		else
		{
			// If we already responded, use followup
			await FollowupAsync(errorMessage, ephemeral: true);
		}
	}
}
