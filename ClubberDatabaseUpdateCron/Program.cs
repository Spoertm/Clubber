using Clubber.Configuration;
using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
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
			_client.Log += LoggingService.LogAsync;

			await Task.Delay(-1);
		}

		private static string GetToken()
		{
			string tokenPath = Path.Combine(AppContext.BaseDirectory, "Models", "Token.txt");
			return File.ReadAllText(tokenPath);
		}

		private static async Task OnReady()
		{
			IServiceProvider services = new ServiceCollection()
				.AddSingleton(_client)
				.AddSingleton<DatabaseHelper>()
				.AddSingleton<UpdateRolesHelper>()
				.AddSingleton<DiscordHelper>()
				.BuildServiceProvider();

			string databaseFilePath = services.GetRequiredService<DatabaseHelper>().DatabaseFilePath;
			DiscordHelper discordHelper = services.GetRequiredService<DiscordHelper>();

			List<Exception> exceptionList = new();
			Directory.CreateDirectory(Path.GetDirectoryName(databaseFilePath)!);
			string latestAttachmentUrl = discordHelper.GetLatestAttachmentUrlFromChannel(Config.DatabaseBackupChannelId).Result;
			string databaseJson = WebService.RequestStringAsync(latestAttachmentUrl).Result;
			await File.WriteAllTextAsync(databaseJson, databaseFilePath);

			SocketGuild? ddPals = _client.GetGuild(Config.DdPalsId);
			IMessageChannel? cronUpdateChannel = _client.GetChannel(Config.CronUpdateChannelId) as IMessageChannel;

			const string checkingString = "Checking for role updates...";
			IUserMessage msg = await cronUpdateChannel!.SendMessageAsync(checkingString);

			int tries = 0;
			const int maxTries = 5;

			bool success = false;
			do
			{
				try
				{
					(string repsonseMessage, Embed[] responseRoleUpdateEmbeds) = await services.GetRequiredService<UpdateRolesHelper>().UpdateRolesAndDb(ddPals.Users);

					await msg.ModifyAsync(m => m.Content = $"{checkingString}\n{repsonseMessage}");

					for (int i = 0; i < responseRoleUpdateEmbeds.Length; i++)
						await cronUpdateChannel.SendMessageAsync(null, false, responseRoleUpdateEmbeds[i]);

					success = true;
				}
				catch (Exception ex)
				{
					exceptionList.Add(ex);
					tries++;
					if (tries > maxTries)
					{
						await cronUpdateChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");

						SocketTextChannel? clubberExceptionsChannel = _client.GetChannel(Config.ClubberExceptionsChannelId) as SocketTextChannel;
						foreach (Exception exc in exceptionList.GroupBy(e => e.ToString()).Select(group => group.First()))
							await clubberExceptionsChannel!.SendMessageAsync(null, false, EmbedHelper.Exception(exc));

						break;
					}

					await cronUpdateChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
					Thread.Sleep(10000); // Sleep 10s
				}
			}
			while (!success);

			await _client.StopAsync();
			Thread.Sleep(1000);
			await _client.LogoutAsync();
			Environment.Exit(0);
		}
	}
}