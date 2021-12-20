﻿using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Clubber.BackgroundTasks;

public class DatabaseUpdateService : ExactBackgroundService
{
	private readonly IDiscordHelper _discordHelper;
	private readonly UpdateRolesHelper _updateRolesHelper;

	public DatabaseUpdateService(
		IDiscordHelper discordHelper,
		UpdateRolesHelper updateRolesHelper,
		LoggingService loggingService)
		: base(loggingService)
	{
		_discordHelper = discordHelper;
		_updateRolesHelper = updateRolesHelper;
	}

	protected override TimeOnly UtcTriggerTime => new(16, 00);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		SocketGuild ddPals = _discordHelper.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("DdPalsId")!)) ?? throw new("DD Pals server not found with the provided ID.");
		SocketTextChannel dailyUpdateChannel = _discordHelper.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("DailyUpdateChannelId")!));
		IUserMessage msg = await dailyUpdateChannel.SendMessageAsync("Checking for role updates...");

		List<Exception> exceptionList = new();
		SocketTextChannel clubberExceptionsChannel = _discordHelper.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("ClubberExceptionsChannelId")!));
		int tries = 0;
		const int maxTries = 5;
		bool success = false;
		do
		{
			try
			{
				(string repsonseMessage, Embed[] responseRoleUpdateEmbeds) = await _updateRolesHelper.UpdateRolesAndDb(ddPals.Users);
				await msg.ModifyAsync(m => m.Content = repsonseMessage);
				for (int i = 0; i < responseRoleUpdateEmbeds.Length; i++)
					await dailyUpdateChannel.SendMessageAsync(embed: responseRoleUpdateEmbeds[i]);

				success = true;
			}
			catch (Exception ex)
			{
				exceptionList.Add(ex);
				if (tries++ > maxTries)
				{
					await dailyUpdateChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");
					foreach (Exception exc in exceptionList.GroupBy(e => e.ToString()).Select(group => group.First()))
						await clubberExceptionsChannel.SendMessageAsync(embed: EmbedHelper.Exception(exc));

					break;
				}

				await dailyUpdateChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
				Thread.Sleep(10000); // Sleep 10s
			}
		}
		while (!success);
	}
}
