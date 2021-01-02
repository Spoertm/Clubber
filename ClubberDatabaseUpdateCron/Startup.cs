using Clubber;
using Clubber.Databases;
using Clubber.Helpers;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ClubberDatabaseUpdateCron
{
	public class Startup
	{
		private DiscordSocketClient _client;
		private ServiceProvider _provider;

		public async Task RunAsync()
		{
			ServiceCollection services = new();
			ConfigureServices(services);

			_provider = services.BuildServiceProvider();
			_client = _provider.GetRequiredService<DiscordSocketClient>();

			const string discordToken = "NzQzNDMxNTAyODQyMjk4MzY4.XzUkig.UQrKlF7axeeFqewonpkTTAwaIIo";
			await _client.LoginAsync(TokenType.Bot, discordToken);
			await _client.StartAsync();

			_client.Ready += OnReady;

			await Task.Delay(-1);
		}

		public async Task OnReady()
		{
			SocketGuild ddPals = _client.GetGuild(399568958669455364);
			SocketTextChannel testingChannel = ddPals.GetTextChannel(447487662891466752);
			Stopwatch stopwatch = new Stopwatch();

			int tries = 1;
			const int maxTries = 5;
			await testingChannel.SendMessageAsync("🗡 Attempting database update...");

			UpdateRolesHelper updateRoleHelper = _provider.GetRequiredService<UpdateRolesHelper>();

			bool success = false;
			do
			{
				try
				{
					stopwatch.Restart();

					UpdateRolesHelper.UpdateRolesResponse response = await updateRoleHelper.UpdateRolesAndDb();
					success = true;

					if (response.NonMemberCount > 0)
						await testingChannel.SendMessageAsync($"ℹ️ Unable to update {response.NonMemberCount} user(s). They're most likely not in the server.");

					if (response.UpdatedUsers > 0)
						await testingChannel.SendMessageAsync($"✅ Successfully updated database and member roles for {response.UpdatedUsers} users.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
					else
						await testingChannel.SendMessageAsync($"No role updates were needed.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
				}
				catch
				{
					tries++;
					if (tries > maxTries)
					{
						await testingChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");
						break;
					}
					else
					{
						await testingChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
						System.Threading.Thread.Sleep(10000); // Sleep 10s
					}
				}
			}
			while (!success);

			Environment.Exit(0);
		}

		private static void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Error,
				MessageCacheSize = 1000,
				AlwaysDownloadUsers = true,
			}))
			.AddSingleton(new CommandService(new CommandServiceConfig
			{
				LogLevel = LogSeverity.Error,
				DefaultRunMode = RunMode.Async,
				CaseSensitiveCommands = false,
				IgnoreExtraArgs = true,
			}))
			.AddSingleton<InteractiveService>()
			.AddSingleton<CommandHandler>()
			.AddSingleton<StartupService>()
			.AddSingleton<LoggingService>()
			.AddSingleton<MongoDatabase>()
			.AddSingleton<ScoreRoles>()
			.AddSingleton<UpdateRolesHelper>()
			.AddSingleton<DatabaseHelper>();
		}
	}
}
