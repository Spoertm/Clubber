using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Clubber.Domain.Models;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Diagnostics;

namespace Clubber.Discord.Services;

public sealed class DatabaseUpdateService(IOptions<AppConfig> config, IServiceScopeFactory services) : ExactBackgroundService
{
	private readonly AppConfig _config = config.Value;

	protected override TimeOnly UtcTriggerTime => new(16, 00);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		await using AsyncServiceScope scope = services.CreateAsyncScope();
		ScoreRoleService scoreRoleService = scope.ServiceProvider.GetRequiredService<ScoreRoleService>();
		IDiscordHelper discordHelper = scope.ServiceProvider.GetRequiredService<IDiscordHelper>();

		SocketGuild ddPals = discordHelper.GetGuild(_config.DdPalsId) ?? throw new Exception("DD Pals server not found with the provided ID.");
		SocketTextChannel dailyUpdateLoggingChannel = discordHelper.GetTextChannel(_config.DailyUpdateLoggingChannelId);
		RestUserMessage msg = await dailyUpdateLoggingChannel.SendMessageAsync("Checking for role updates...");

		int tries = 0;
		const int maxTries = 5;
		bool success = false;
		do
		{
			try
			{
				tries++;

				Stopwatch sw = Stopwatch.StartNew();
				BulkUserRoleUpdates bulkUpdateResponse = await scoreRoleService.GetBulkUserRoleUpdates(ddPals.Users);
				sw.Stop();

				string message = bulkUpdateResponse.UserRoleUpdates.Count > 0
					? $"✅ Successfully updated database and {bulkUpdateResponse.UserRoleUpdates.Count} user(s).\n🕐 Execution took {sw.ElapsedMilliseconds} ms."
					: $"No updates needed today.\nExecution took {sw.ElapsedMilliseconds} ms.";

				message += $"\nℹ️ {bulkUpdateResponse.NonMemberCount} user(s) are registered but aren't in the server.";
				await msg.ModifyAsync(m => m.Content = message);

				List<UserRoleUpdate> successfulUpdates = [];
				int skippedUsers = 0;

				foreach (UserRoleUpdate roleUpdate in bulkUpdateResponse.UserRoleUpdates)
				{
					SocketGuildUser? refreshedUser = ddPals.GetUser(roleUpdate.User.Id);
					if (refreshedUser == null)
					{
						Log.Information("Skipping role update for user {UserId} ({Username}) - user no longer found in server",
							roleUpdate.User.Id, roleUpdate.User.AvailableNameSanitized());
						skippedUsers++;
						continue;
					}

					try
					{
						if (roleUpdate.RoleUpdate.RolesToAdd.Count > 0)
						{
							await refreshedUser.AddRolesAsync(roleUpdate.RoleUpdate.RolesToAdd);
						}

						if (roleUpdate.RoleUpdate.RolesToRemove.Count > 0)
						{
							await refreshedUser.RemoveRolesAsync(roleUpdate.RoleUpdate.RolesToRemove);
						}

						successfulUpdates.Add(roleUpdate with { User = refreshedUser });
					}
					catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMember)
					{
						Log.Information(ex, "Skipping role update for user {UserId} ({Username}) - user left server during update",
							roleUpdate.User.Id, roleUpdate.User.AvailableNameSanitized());
						skippedUsers++;
					}
					catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions)
					{
						Log.Warning(ex, "Failed to update roles for user {UserId} ({Username}) - missing permissions",
							roleUpdate.User.Id, roleUpdate.User.AvailableNameSanitized());
						skippedUsers++;
					}
					catch (HttpException ex)
					{
						Log.Warning(ex, "Failed to update roles for user {UserId} ({Username}) - Discord API error: {ErrorCode}",
							roleUpdate.User.Id, roleUpdate.User.AvailableNameSanitized(), ex.DiscordCode);
						skippedUsers++;
					}
				}

				if (skippedUsers > 0)
				{
					string updatedMessage =
						message + $"\n⚠️ Skipped {skippedUsers} user(s) due to errors (users left server, permission issues, etc.)";
					await msg.ModifyAsync(m => m.Content = updatedMessage);
				}

				Embed[] roleUpdateEmbeds = successfulUpdates
					.Select(EmbedHelper.UpdateRoles)
					.ToArray();

				if (roleUpdateEmbeds.Length > 0)
				{
					string userStr = roleUpdateEmbeds.Length == 1 ? "user" : "users";
					string dailyUpdateMessageStr = $"Updated {roleUpdateEmbeds.Length} {userStr} today.";

					Result dailyChannelResult = await discordHelper.SendEmbedsEfficientlyAsync(
						roleUpdateEmbeds,
						_config.DailyUpdateChannelId,
						dailyUpdateMessageStr);

					if (dailyChannelResult.IsFailure)
					{
						await dailyUpdateLoggingChannel.SendMessageAsync($"❌ Failed to send embeds in DD channel: {dailyChannelResult.ErrorMsg}");
					}

					Result loggingChannelResult = await discordHelper.SendEmbedsEfficientlyAsync(
						roleUpdateEmbeds,
						_config.DailyUpdateLoggingChannelId);

					if (loggingChannelResult.IsFailure)
					{
						await dailyUpdateLoggingChannel.SendMessageAsync(
							$"❌ Failed to send embeds in logging channel: {dailyChannelResult.ErrorMsg}");
					}
				}

				success = true;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "DB update procedure failed");
				if (tries >= maxTries)
				{
					await dailyUpdateLoggingChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");
					return;
				}

				await dailyUpdateLoggingChannel.SendMessageAsync($"⚠ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
				await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Sleep 10s
			}
		} while (!success);
	}
}
