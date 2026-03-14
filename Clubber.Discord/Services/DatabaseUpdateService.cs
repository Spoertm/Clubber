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

		SocketGuild ddPals = discordHelper.GetGuild(_config.DdPalsId)
			?? throw new Exception("DD Pals server not found with the provided ID.");
		SocketTextChannel loggingChannel = discordHelper.GetTextChannel(_config.DailyUpdateLoggingChannelId);
		RestUserMessage statusMsg = await loggingChannel.SendMessageAsync("Checking for role updates...");

		// Execute with retry logic
		Result<BulkUserRoleUpdates> result = await ExecuteWithRetryAsync(
			() => ExecuteUpdateAsync(scoreRoleService, discordHelper, ddPals, statusMsg),
			maxRetries: 5,
			onRetry: async (attempt, ex) =>
			{
				Log.Error(ex, "DB update attempt {Attempt} failed", attempt);
				await loggingChannel.SendMessageAsync($"⚠ ({attempt}/5) Update failed. Trying again in 10s...");
				await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
			});

		if (result.IsFailure)
		{
			await loggingChannel.SendMessageAsync("❌ Failed to update DB after 5 attempts.");
		}
	}

	private async Task<Result<BulkUserRoleUpdates>> ExecuteUpdateAsync(
		ScoreRoleService scoreRoleService,
		IDiscordHelper discordHelper,
		SocketGuild ddPals,
		RestUserMessage statusMsg)
	{
		Stopwatch sw = Stopwatch.StartNew();
		BulkUserRoleUpdates bulkUpdate = await scoreRoleService.GetBulkUserRoleUpdates(ddPals.Users);
		sw.Stop();

		List<UserRoleUpdate> successfulUpdates = [];
		int skippedUsers = 0;

		foreach (UserRoleUpdate update in bulkUpdate.UserRoleUpdates)
		{
			SocketGuildUser? user = ddPals.GetUser(update.User.Id);
			if (user == null)
			{
				Log.Information("Skipping role update for user {UserId} - user no longer in server", update.User.Id);
				skippedUsers++;
				continue;
			}

			try
			{
				if (update.RoleChange.RolesToAdd.Count > 0)
					await user.AddRolesAsync(update.RoleChange.RolesToAdd);

				if (update.RoleChange.RolesToRemove.Count > 0)
					await user.RemoveRolesAsync(update.RoleChange.RolesToRemove);

				successfulUpdates.Add(update with { User = user });
			}
			catch (HttpException ex) when (ex.DiscordCode is DiscordErrorCode.UnknownMember or DiscordErrorCode.MissingPermissions)
			{
				Log.Information("Skipping user {UserId}: {Error}", update.User.Id, ex.DiscordCode);
				skippedUsers++;
			}
			catch (HttpException ex)
			{
				Log.Warning("Failed to update roles for user {UserId}: {Error}", update.User.Id, ex.DiscordCode);
				skippedUsers++;
			}
		}

		// Update status message
		string message = BuildStatusMessage(successfulUpdates.Count, bulkUpdate.NonMemberCount, skippedUsers, sw.ElapsedMilliseconds);
		await statusMsg.ModifyAsync(m => m.Content = message);

		// Send embeds to channels
		if (successfulUpdates.Count > 0)
		{
			Embed[] embeds = [.. successfulUpdates.Select(EmbedHelper.UpdateRoles)];
			await SendRoleUpdateEmbedsAsync(discordHelper, embeds);
		}

		return Result.Success(bulkUpdate);
	}

	private static async Task<Result<T>> ExecuteWithRetryAsync<T>(
		Func<Task<Result<T>>> action,
		int maxRetries,
		Func<int, Exception, Task> onRetry)
		where T : notnull
	{
		for (int attempt = 1; attempt <= maxRetries; attempt++)
		{
			try
			{
				return await action();
			}
			catch (Exception ex) when (attempt < maxRetries)
			{
				await onRetry(attempt, ex);
			}
		}

		// Final attempt
		try
		{
			return await action();
		}
		catch (Exception ex)
		{
			return Result.Failure<T>(ex.Message);
		}
	}

	private static string BuildStatusMessage(int successCount, int nonMemberCount, int skippedCount, long elapsedMs)
	{
		string message = successCount > 0
			? $"✅ Successfully updated database and {successCount} user(s).\n🕐 Execution took {elapsedMs} ms."
			: $"No updates needed today.\nExecution took {elapsedMs} ms.";

		message += $"\nℹ️ {nonMemberCount} user(s) are registered but aren't in the server.";

		if (skippedCount > 0)
			message += $"\n⚠️ Skipped {skippedCount} user(s) due to errors.";

		return message;
	}

	private async Task SendRoleUpdateEmbedsAsync(IDiscordHelper discordHelper, Embed[] embeds)
	{
		string userStr = embeds.Length == 1 ? "user" : "users";

		Result dailyResult = await discordHelper.SendEmbedsEfficientlyAsync(
			embeds, _config.DailyUpdateChannelId, $"Updated {embeds.Length} {userStr} today.");

		if (dailyResult.IsFailure)
		{
			SocketTextChannel logCh = discordHelper.GetTextChannel(_config.DailyUpdateLoggingChannelId);
			await logCh.SendMessageAsync($"❌ Failed to send embeds in daily channel: {dailyResult.ErrorMsg}");
		}

		Result loggingResult = await discordHelper.SendEmbedsEfficientlyAsync(
			embeds, _config.DailyUpdateLoggingChannelId);

		if (loggingResult.IsFailure)
		{
			SocketTextChannel logCh = discordHelper.GetTextChannel(_config.DailyUpdateLoggingChannelId);
			await logCh.SendMessageAsync($"❌ Failed to send embeds in logging channel: {loggingResult.ErrorMsg}");
		}
	}
}
