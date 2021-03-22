using Clubber;
using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClubberDatabaseUpdateCron
{
	public static class Program
	{
		private const ulong _modsChannelId = 701124439990993036;
		private static DiscordSocketClient _client = null!;

		public static void Main() => RunAsync().GetAwaiter().GetResult();

		public static async Task RunAsync()
		{
			_client = new DiscordSocketClient(new DiscordSocketConfig() { AlwaysDownloadUsers = true, LogLevel = LogSeverity.Info });

			await _client.LoginAsync(TokenType.Bot, Constants.Token);
			await _client.StartAsync();
			_client.Ready += OnReady;

			await Task.Delay(-1);
		}

		public static async Task OnReady()
		{
			IServiceProvider services = new ServiceCollection()
				.AddSingleton(_client)
				.AddSingleton<LoggingService>()
				.AddSingleton<IOService>()
				.AddSingleton<DatabaseHelper>()
				.AddSingleton<UpdateRolesHelper>()
				.AddSingleton<WebService>()
				.BuildServiceProvider();

			ActivatorUtilities.GetServiceOrCreateInstance<LoggingService>(services);

			List<Exception> exceptionList = new();
			await services.GetRequiredService<IOService>().GetDatabaseFileIntoFolder();

			SocketGuild? ddPals = _client.GetGuild(Constants.DdPals);
			IMessageChannel? modsChannel = _client.GetChannel(_modsChannelId) as IMessageChannel;

			const string checkingString = "Checking for role updates...";
			IUserMessage msg = await modsChannel!.SendMessageAsync(checkingString);

			int tries = 0;
			const int maxTries = 5;

			bool success = false;
			do
			{
				try
				{
					DatabaseUpdateResponse response = await services.GetRequiredService<UpdateRolesHelper>().UpdateRolesAndDb(ddPals.Users);

					await msg.ModifyAsync(m => m.Content = $"{checkingString}\n{response.Message}");

					for (int i = 0; i < response.RoleUpdateEmbeds.Length; i++)
						await modsChannel.SendMessageAsync(null, false, response.RoleUpdateEmbeds[i]);

					success = true;
				}
				catch (Exception ex)
				{
					exceptionList.Add(ex);
					tries++;
					if (tries > maxTries)
					{
						await modsChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");

						SocketTextChannel? clubberExceptionsChannel = _client.GetChannel(Constants.ClubberExceptionsChannel) as SocketTextChannel;
						foreach (Exception exc in exceptionList.GroupBy(e => e.ToString()).Select(group => group.First()))
							await clubberExceptionsChannel!.SendMessageAsync(null, false, EmbedHelper.Exception(exc));

						break;
					}
					else
					{
						await modsChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
						System.Threading.Thread.Sleep(10000); // Sleep 10s
					}
				}
			}
			while (!success);

			Environment.Exit(0);
		}
	}
}
