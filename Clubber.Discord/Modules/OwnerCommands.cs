using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Models;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Diagnostics;

namespace Clubber.Discord.Modules;

[RequireOwner]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public sealed class OwnerCommands(ScoreRoleService scoreRoleService, IDiscordHelper discordHelper)
	: InteractionModuleBase<SocketInteractionContext>
{
	[SlashCommand("update-database", "Force update the database and user roles")]
	public async Task UpdateDatabase()
	{
		await DeferAsync();

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
			await FollowupAsync("Failed to update database.", ephemeral: true);
		}
	}

	[SlashCommand("post-welcome", "Post the welcome/registration embed in the current channel")]
	public async Task PostWelcome()
	{
		try
		{
			await RespondAsync("Posting welcome message...");
			await Context.Channel.SendMessageAsync(embeds: EmbedHelper.RegisterEmbeds());
		}
		catch (Exception ex)
		{
			Serilog.Log.Error(ex, "Error posting welcome message");
			await RespondAsync("Failed to post welcome message.", ephemeral: true);
		}
	}
}
