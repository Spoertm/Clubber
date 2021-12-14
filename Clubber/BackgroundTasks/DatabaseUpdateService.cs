using Clubber.Configuration;
using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Clubber.BackgroundTasks;

public class DatabaseUpdateService : AbstractBackgroundService
{
	private readonly IConfig _config;
	private readonly IDiscordHelper _discordHelper;
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IWebService _webService;
	private readonly UpdateRolesHelper _updateRolesHelper;

	public DatabaseUpdateService(
		IConfig config,
		IDiscordHelper discordHelper,
		IDatabaseHelper databaseHelper,
		IWebService webService,
		UpdateRolesHelper updateRolesHelper,
		LoggingService loggingService)
		: base(loggingService)
	{
		_config = config;
		_discordHelper = discordHelper;
		_databaseHelper = databaseHelper;
		_webService = webService;
		_updateRolesHelper = updateRolesHelper;
	}

	protected override TimeSpan Interval => TimeSpan.FromDays(1);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_databaseHelper.DatabaseFilePath)!);
		string latestAttachmentUrl = await _discordHelper.GetLatestAttachmentUrlFromChannel(_config.DatabaseBackupChannelId);
		string databaseJson = await _webService.RequestStringAsync(latestAttachmentUrl);
		await File.WriteAllTextAsync(_databaseHelper.DatabaseFilePath, databaseJson, stoppingToken);

		SocketGuild ddPals = _discordHelper.GetGuild(_config.DdPalsId) ?? throw new($"DD Pals server not found with ID {_config.DdPalsId}");
		SocketTextChannel cronUpdateChannel = _discordHelper.GetTextChannel(_config.CronUpdateChannelId);
		IUserMessage msg = await cronUpdateChannel.SendMessageAsync("Checking for role updates...");

		List<Exception> exceptionList = new();
		SocketTextChannel clubberExceptionsChannel = _discordHelper.GetTextChannel(_config.ClubberExceptionsChannelId);
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
					await cronUpdateChannel.SendMessageAsync(embed: responseRoleUpdateEmbeds[i]);

				success = true;
			}
			catch (Exception ex)
			{
				exceptionList.Add(ex);
				if (tries++ > maxTries)
				{
					await cronUpdateChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");
					foreach (Exception exc in exceptionList.GroupBy(e => e.ToString()).Select(group => group.First()))
						await clubberExceptionsChannel.SendMessageAsync(embed: EmbedHelper.Exception(exc));

					break;
				}

				await cronUpdateChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
				Thread.Sleep(10000); // Sleep 10s
			}
		}
		while (!success);
	}
}
