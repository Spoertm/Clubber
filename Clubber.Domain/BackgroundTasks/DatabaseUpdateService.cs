﻿using Clubber.Domain.Helpers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Clubber.Domain.BackgroundTasks;

public class DatabaseUpdateService : ExactBackgroundService
{
	private readonly IConfiguration _config;
	private readonly IServiceScopeFactory _services;

	public DatabaseUpdateService(IConfiguration config, IServiceScopeFactory services)
	{
		_config = config;
		_services = services;
	}

	protected override TimeOnly UtcTriggerTime => new(16, 00);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		await using AsyncServiceScope scope = _services.CreateAsyncScope();
		UpdateRolesHelper updateRolesHelper = scope.ServiceProvider.GetRequiredService<UpdateRolesHelper>();
		IDiscordHelper discordHelper = scope.ServiceProvider.GetRequiredService<IDiscordHelper>();

		SocketGuild ddPals = discordHelper.GetGuild(_config.GetValue<ulong>("DdPalsId")) ?? throw new("DD Pals server not found with the provided ID.");
		SocketTextChannel dailyUpdateLoggingChannel = discordHelper.GetTextChannel(_config.GetValue<ulong>("DailyUpdateLoggingChannelId"));
		SocketTextChannel dailyUpdateChannel = discordHelper.GetTextChannel(_config.GetValue<ulong>("DailyUpdateChannelId"));
		IUserMessage msg = await dailyUpdateLoggingChannel.SendMessageAsync("Checking for role updates...");

		int tries = 0;
		const int maxTries = 5;
		bool success = false;
		do
		{
			try
			{
				tries++;

				(string repsonseMessage, Embed[] responseRoleUpdateEmbeds) = await updateRolesHelper.UpdateRolesAndDb(ddPals.Users);
				await msg.ModifyAsync(m => m.Content = repsonseMessage);

				if (responseRoleUpdateEmbeds.Length > 0)
				{
					await SendEmbedsEfficientlyAsync(
						responseRoleUpdateEmbeds,
						dailyUpdateLoggingChannel,
						dailyUpdateChannel,
						stoppingToken);
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

	private static async Task SendEmbedsEfficientlyAsync(
		Embed[] responseRoleUpdateEmbeds,
		SocketTextChannel dailyUpdateLoggingChannel,
		SocketTextChannel dailyUpdateChannel,
		CancellationToken stoppingToken)
	{
		IEnumerable<Embed[]> embedChunks = responseRoleUpdateEmbeds.Chunk(DiscordConfig.MaxEmbedsPerMessage);
		foreach (Embed[] embedChunk in embedChunks)
		{
			await Task.Delay(1000, stoppingToken);

			await dailyUpdateLoggingChannel.SendMessageAsync(embeds: embedChunk);

			await Task.Delay(1000, stoppingToken);

			string userStr = responseRoleUpdateEmbeds.Length == 1 ? "user" : "users";
			string dailyUpdateMessageStr = $"Updated {responseRoleUpdateEmbeds.Length} {userStr} today.";

			await dailyUpdateChannel.SendMessageAsync(dailyUpdateMessageStr, embeds: embedChunk);
		}
	}
}
