﻿using Clubber.Databases;
using Clubber.Helpers;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Clubber
{
	public class Startup
	{
		public IConfigurationRoot Configuration { get; }

		public Startup(string[] args)
		{
			IConfigurationBuilder builder = new ConfigurationBuilder()        // Create a new instance of the config builder
				.SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
				.AddYamlFile("_config.yml");                // Add this (yaml encoded) file to the configuration
			Configuration = builder.Build();                // Build the configuration
		}

		public static async Task RunAsync(string[] args)
		{
			Startup startup = new(args);
			await startup.RunAsync();
		}

		public async Task RunAsync()
		{
			ServiceCollection services = new();             // Create a new instance of a service collection
			ConfigureServices(services);

			ServiceProvider provider = services.BuildServiceProvider();     // Build the service provider
			provider.GetRequiredService<LoggingService>();      // Start the logging service
			provider.GetRequiredService<CommandHandler>();      // Start the command handler service

			await provider.GetRequiredService<StartupService>().StartAsync();       // Start the startup service
			await Task.Delay(-1);                               // Keep the program alive
		}

		private void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
			{                                       // Add discord to the collection
				LogLevel = LogSeverity.Error,       // Tell the logger to give Verbose amount of info
				MessageCacheSize = 1000,            // Cache 1,000 messages per channel
				AlwaysDownloadUsers = true,
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
			.AddSingleton<ChartHelper>()
			.AddSingleton(Configuration);
		}
	}
}
