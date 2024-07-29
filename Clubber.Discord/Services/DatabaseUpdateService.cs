using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Clubber.Domain.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Diagnostics;

namespace Clubber.Discord.Services;

public class DatabaseUpdateService : ExactBackgroundService
{
	private readonly AppConfig _config;
	private readonly IServiceScopeFactory _services;

	public DatabaseUpdateService(IOptions<AppConfig> config, IServiceScopeFactory services)
	{
		_config = config.Value;
		_services = services;
	}

	protected override TimeOnly UtcTriggerTime => new(16, 00);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		await using AsyncServiceScope scope = _services.CreateAsyncScope();
		ScoreRoleService scoreRoleService = scope.ServiceProvider.GetRequiredService<ScoreRoleService>();
		IDiscordHelper discordHelper = scope.ServiceProvider.GetRequiredService<IDiscordHelper>();

		SocketGuild ddPals = discordHelper.GetGuild(_config.DdPalsId) ?? throw new("DD Pals server not found with the provided ID.");
		SocketTextChannel dailyUpdateLoggingChannel = discordHelper.GetTextChannel(_config.DailyUpdateLoggingChannelId);
		IUserMessage msg = await dailyUpdateLoggingChannel.SendMessageAsync("Checking for role updates...");

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

				Embed[] roleUpdateEmbeds = bulkUpdateResponse.UserRoleUpdates
					.Select(EmbedHelper.UpdateRoles)
					.ToArray();

				if (roleUpdateEmbeds.Length > 0)
				{
					string userStr = roleUpdateEmbeds.Length == 1 ? "user" : "users";
					string dailyUpdateMessageStr = $"Updated {roleUpdateEmbeds.Length} {userStr} today.";

					Result dailyChannelResult = await discordHelper.SendEmbedsEfficientlyAsync(
						roleUpdateEmbeds,
						_config.DailyUpdateChannel,
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
						await dailyUpdateLoggingChannel.SendMessageAsync($"❌ Failed to send embeds in logging channel: {dailyChannelResult.ErrorMsg}");
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
		}
		while (!success);
	}
}
