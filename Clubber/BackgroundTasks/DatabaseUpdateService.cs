﻿using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Clubber.BackgroundTasks;

public class DatabaseUpdateService : AbstractBackgroundService
{
	private readonly IConfiguration _config;
	private readonly IDiscordHelper _discordHelper;
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IWebService _webService;
	private readonly UpdateRolesHelper _updateRolesHelper;
	private readonly DatabaseService _dbContext;

	public DatabaseUpdateService(
		IConfiguration config,
		IDiscordHelper discordHelper,
		IDatabaseHelper databaseHelper,
		IWebService webService,
		UpdateRolesHelper updateRolesHelper,
		LoggingService loggingService,
		DatabaseService dbContext)
		: base(loggingService)
	{
		_config = config;
		_discordHelper = discordHelper;
		_databaseHelper = databaseHelper;
		_webService = webService;
		_updateRolesHelper = updateRolesHelper;
		_dbContext = dbContext;
	}

	protected override TimeSpan Interval => TimeSpan.FromDays(1);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		SocketGuild ddPals = _discordHelper.GetGuild(_config.GetValue<ulong>("DdPalsId")) ?? throw new($"DD Pals server not found with ID {_config["DdPalsId"]}");
		SocketTextChannel dailyUpdateChannel = _discordHelper.GetTextChannel(_config.GetValue<ulong>("DailyUpdateChannelId"));
		IUserMessage msg = await dailyUpdateChannel.SendMessageAsync("Checking for role updates...");

		List<Exception> exceptionList = new();
		SocketTextChannel clubberExceptionsChannel = _discordHelper.GetTextChannel(_config.GetValue<ulong>("ClubberExceptionsChannelId"));
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
