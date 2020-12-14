using Clubber;
using Clubber.Databases;
using Clubber.Helpers;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace ClubberDatabaseUpdateCron
{
	public class Startup
	{
		DiscordSocketClient Client;
		ServiceProvider Provider;
		public IConfigurationRoot Configuration { get; }

		public Startup(string[] args)
		{
			var builder = new ConfigurationBuilder()        // Create a new instance of the config builder
				.SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
				.AddYamlFile("_config.yml");                // Add this (yaml encoded) file to the configuration
			Configuration = builder.Build();                // Build the configuration
		}

		public static async Task RunAsync(string[] args)
		{
			var startup = new Startup(args);
			await startup.RunAsync();
		}

		public async Task RunAsync()
		{
			var services = new ServiceCollection(); // Create a new instance of a service collection
			ConfigureServices(services);

			Provider = services.BuildServiceProvider(); // Build the service provider
			Client = Provider.GetRequiredService<DiscordSocketClient>();

			string discordToken = "NzQzNDMxNTAyODQyMjk4MzY4.XzUkig.UQrKlF7axeeFqewonpkTTAwaIIo";
			await Client.LoginAsync(TokenType.Bot, discordToken);
			await Client.StartAsync();

			Client.Ready += OnReady;

			await Task.Delay(-1);
		}

		public async Task OnReady()
		{
			SocketGuild ddPals = Client.GetGuild(399568958669455364);
			SocketTextChannel testAndConfigChannel = ddPals.GetTextChannel(447487662891466752);

			int tries = 1, maxTries = 5;
			await testAndConfigChannel.SendMessageAsync(":dagger: Attempting database update...");

			var updateRoleHelper = Provider.GetRequiredService<UpdateRolesHelper>();

			bool success = false;
			do
			{
				try
				{
					await updateRoleHelper.UpdateRolesAndDb();
					success = true;
				}
				catch
				{
					tries++;
					if (tries > maxTries)
					{
						await testAndConfigChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");
						break;
					}
					else
					{
						await testAndConfigChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed, possible lock on file. Trying again in 10s...");
						System.Threading.Thread.Sleep(10000); // Sleep 10s
					}
				}

			} while (!success);

			Environment.Exit(0);
		}

		private void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
			{                                       // Add discord to the collection
				LogLevel = LogSeverity.Error,       // Tell the logger to give Verbose amount of info
				MessageCacheSize = 1000,            // Cache 1,000 messages per channel
				AlwaysDownloadUsers = true
			}))
			.AddSingleton(new CommandService(new CommandServiceConfig
			{                                       // Add the command service to the collection
				LogLevel = LogSeverity.Error,       // Tell the logger to give Verbose amount of info
				DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
				CaseSensitiveCommands = false,
				IgnoreExtraArgs = true,
			}))
			.AddSingleton<InteractiveService>()
			.AddSingleton<CommandHandler>()
			.AddSingleton<StartupService>()
			.AddSingleton<LoggingService>()
			.AddSingleton<Random>()
			.AddSingleton<MongoDatabase>()
			.AddSingleton<ScoreRoles>()
			.AddSingleton<UpdateRolesHelper>()
			.AddSingleton<DatabaseHelper>()
			.AddSingleton(Configuration);
		}
	}
}
