using Clubber.Domain.Helpers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Clubber.Domain.BackgroundTasks;

public class DatabaseUpdateService : ExactBackgroundService
{
	private readonly IConfiguration _config;
	private readonly IDiscordHelper _discordHelper;
	private readonly UpdateRolesHelper _updateRolesHelper;

	public DatabaseUpdateService(IConfiguration config, IDiscordHelper discordHelper, UpdateRolesHelper updateRolesHelper)
	{
		_config = config;
		_discordHelper = discordHelper;
		_updateRolesHelper = updateRolesHelper;
	}

	protected override TimeOnly UtcTriggerTime => new(16, 00);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		SocketGuild ddPals = _discordHelper.GetGuild(_config.GetValue<ulong>("DdPalsId")) ?? throw new("DD Pals server not found with the provided ID.");
		SocketTextChannel dailyUpdateChannel = _discordHelper.GetTextChannel(_config.GetValue<ulong>("DailyUpdateChannelId"));
		IUserMessage msg = await dailyUpdateChannel.SendMessageAsync("Checking for role updates...");

		List<Exception> exceptionList = new();
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
						Log.Error(exc, "DB update procedure failed");

					break;
				}

				await dailyUpdateChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
				await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Sleep 10s
			}
		}
		while (!success);
	}
}
