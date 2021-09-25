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

namespace ClubberDatabaseUpdateCron
{
	public static class Program
	{
		private static DiscordSocketClient _client = null!;

		public static void Main() => RunAsync().GetAwaiter().GetResult();

		private static async Task RunAsync()
		{
			_client = new(new() { AlwaysDownloadUsers = true, LogLevel = LogSeverity.Error });

			await _client.LoginAsync(TokenType.Bot, GetToken());
			await _client.StartAsync();
			_client.Ready += OnReady;

			await Task.Delay(-1);
		}

		private static string GetToken()
		{
			string tokenPath = Path.Combine(AppContext.BaseDirectory, "Data", "Token.txt");
			return File.ReadAllText(tokenPath);
		}

		private static async Task OnReady()
		{
			IConfig config = new Config();
			IWebService webService = new WebService();
			LoggingService loggingService = new();
			IDiscordHelper discordHelper = new DiscordHelper(config, _client);
			IDatabaseHelper databaseHelper = new DatabaseHelper(config, discordHelper, new IOService(), webService);
			UpdateRolesHelper updateRolesHelper = new(config, databaseHelper, webService);

			_client.Log += loggingService.LogAsync;

			Directory.CreateDirectory(Path.GetDirectoryName(databaseHelper.DatabaseFilePath)!);
			string latestAttachmentUrl = await discordHelper.GetLatestAttachmentUrlFromChannel(config.DatabaseBackupChannelId);
			string databaseJson = await webService.RequestStringAsync(latestAttachmentUrl);
			await File.WriteAllTextAsync(databaseHelper.DatabaseFilePath, databaseJson);

			SocketGuild ddPals = _client.GetGuild(config.DdPalsId) ?? throw new($"DD Pals server not found with ID {config.DdPalsId}");
			SocketTextChannel cronUpdateChannel = discordHelper.GetTextChannel(config.CronUpdateChannelId);
			IUserMessage msg = await cronUpdateChannel.SendMessageAsync("Checking for role updates...");

			List<Exception> exceptionList = new();
			SocketTextChannel clubberExceptionsChannel = discordHelper.GetTextChannel(config.ClubberExceptionsChannelId);
			int tries = 0;
			const int maxTries = 5;
			bool success = false;
			do
			{
				try
				{
					(string repsonseMessage, Embed[] responseRoleUpdateEmbeds) = await updateRolesHelper.UpdateRolesAndDb(ddPals.Users);
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

			await _client.LogoutAsync();
			await _client.StopAsync();
			Environment.Exit(0);
		}
	}
}