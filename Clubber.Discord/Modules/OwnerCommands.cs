using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System.Diagnostics;

namespace Clubber.Discord.Modules;

[Name("👑 Owner Commands")]
[global::Discord.Interactions.RequireOwner]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public sealed class OwnerCommands(ScoreRoleService scoreRoleService, IDiscordHelper discordHelper)
	: InteractionModuleBase<SocketInteractionContext>
{
	[SlashCommand("update-database", "Force update the database and user roles")]
	public async Task UpdateDatabase()
	{
		// Defer immediately to prevent timeout issues
		try
		{
			await DeferAsync();
		}
		catch (Exception ex)
		{
			Serilog.Log.Error(ex, "Failed to defer interaction for update-database command");
			// If we can't defer, try to respond directly instead
			try
			{
				await RespondAsync("❌ Failed to start database update (interaction error). Please try again.", ephemeral: true);
			}
			catch
			{
				// If both defer and respond fail, there's nothing we can do
				Serilog.Log.Error("Failed to respond to update-database command after defer failure");
			}

			return;
		}

		try
		{
			SocketGuild guild = Context.Guild;

			Stopwatch sw = Stopwatch.StartNew();
			BulkUserRoleUpdates response = await scoreRoleService.GetBulkUserRoleUpdates(guild.Users);
			sw.Stop();

			string message = response.UserRoleUpdates.Count > 0
				? $"✅ Successfully updated database and {response.UserRoleUpdates.Count} user(s).\n🕐 Execution took {sw.ElapsedMilliseconds} ms."
				: $"No updates needed today.\nExecution took {sw.ElapsedMilliseconds} ms.";

			message += $"\nℹ️ {response.NonMemberCount} user(s) are registered but aren't in the server.";

			await FollowupAsync(message);

			if (response.UserRoleUpdates.Count == 0)
			{
				return;
			}

			foreach (UserRoleUpdate roleUpdate in response.UserRoleUpdates)
			{
				if (roleUpdate.RoleUpdate.RolesToAdd.Count > 0)
				{
					await roleUpdate.User.AddRolesAsync(roleUpdate.RoleUpdate.RolesToAdd);
				}

				if (roleUpdate.RoleUpdate.RolesToRemove.Count > 0)
				{
					await roleUpdate.User.RemoveRolesAsync(roleUpdate.RoleUpdate.RolesToRemove);
				}
			}

			Embed[] roleUpdateEmbeds = response.UserRoleUpdates
				.Select(EmbedHelper.UpdateRoles)
				.ToArray();

			Result result = await discordHelper.SendEmbedsEfficientlyAsync(roleUpdateEmbeds, Context.Channel.Id);
			if (result.IsFailure)
			{
				await FollowupAsync($"Failed to send role update embeds: {result.ErrorMsg}");
			}
		}
		catch (Exception ex)
		{
			Serilog.Log.Error(ex, "Error updating database");
			try
			{
				await FollowupAsync("❌ Failed to update database. Check logs for details.", ephemeral: true);
			}
			catch (Exception followupEx)
			{
				Serilog.Log.Error(followupEx, "Failed to send error followup message");
			}
		}
	}

	[SlashCommand("post-welcome", "Post the welcome/registration embed in the current channel")]
	public async Task PostWelcome()
	{
		try
		{
			await DeferAsync();
		}
		catch (Exception ex)
		{
			Serilog.Log.Error(ex, "Failed to defer interaction for post-welcome command");
			try
			{
				await RespondAsync("❌ Failed to start welcome post (interaction error). Please try again.", ephemeral: true);
			}
			catch
			{
				Serilog.Log.Error("Failed to respond to post-welcome command after defer failure");
			}

			return;
		}

		try
		{
			await FollowupAsync("Posting welcome message...");
			await Context.Channel.SendMessageAsync(embeds: EmbedHelper.RegisterEmbeds());
		}
		catch (Exception ex)
		{
			Serilog.Log.Error(ex, "Error posting welcome message");
			try
			{
				await FollowupAsync("❌ Failed to post welcome message. Check logs for details.", ephemeral: true);
			}
			catch (Exception followupEx)
			{
				Serilog.Log.Error(followupEx, "Failed to send error followup message");
			}
		}
	}
}
