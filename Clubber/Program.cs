﻿using Clubber.BackgroundTasks;
using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Clubber
{
	public static class Program
	{
		private static readonly CancellationTokenSource _source = new();

		private static async Task Main()
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			AppDomain.CurrentDomain.ProcessExit += StopBot;

			DiscordSocketClient client = new(new() { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true, LogLevel = LogSeverity.Error });
			CommandService commands = new(new() { IgnoreExtraArgs = true, CaseSensitiveCommands = false, DefaultRunMode = RunMode.Async });

			IHost host = ConfigureServices(client, commands).Build();
			IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

			await client.LoginAsync(TokenType.Bot, config["BotToken"]);
			await client.StartAsync();
			await client.SetGameAsync("your roles", null, ActivityType.Watching);
			await commands.AddModulesAsync(Assembly.GetEntryAssembly(), host.Services);

			host.Services.GetRequiredService<MessageHandlerService>();
			host.Services.GetRequiredService<IDatabaseHelper>();
			host.Services.GetRequiredService<WelcomeMessage>();
			host.Services.GetRequiredService<LoggingService>();

			try
			{
				await host.RunAsync(_source.Token);
			}
			catch (TaskCanceledException)
			{
				await client.LogoutAsync();
				client.Dispose();
			}
			finally
			{
				_source.Dispose();
				AppDomain.CurrentDomain.ProcessExit -= StopBot;
			}
		}

		private static IHostBuilder ConfigureServices(DiscordSocketClient client, CommandService commands)
			=> Host.CreateDefaultBuilder()
				.ConfigureAppConfiguration((_, config) => config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
				.ConfigureServices(services =>
					services.AddSingleton(client)
						.AddSingleton(commands)
						.AddSingleton<MessageHandlerService>()
						.AddSingleton<IDatabaseHelper, DatabaseHelper>()
						.AddSingleton<UpdateRolesHelper>()
						.AddSingleton<IDiscordHelper, DiscordHelper>()
						.AddSingleton<UserService>()
						.AddSingleton<IWebService, WebService>()
						.AddSingleton<LoggingService>()
						.AddSingleton<WelcomeMessage>()
						.AddSingleton<ImageGenerator>()
						.AddHostedService<DdNewsPostService>()
						.AddHostedService<DatabaseUpdateService>()
						.AddDbContext<DatabaseService>())
				.ConfigureLogging(logging => logging.ClearProviders());

		private static void StopBot(object? sender, EventArgs e) => StopBot();

		public static void StopBot() => _source.Cancel();
	}
}
