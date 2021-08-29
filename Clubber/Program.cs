using Clubber.BackgroundTasks;
using Clubber.Configuration;
using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
		private static DiscordSocketClient _client = null!;
		private static CommandService _commands = null!;
		private static readonly CancellationTokenSource _source = new();

		private static void Main() => RunBotAsync().GetAwaiter().GetResult();

		private static async Task RunBotAsync()
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

			_client = new(new() { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true, LogLevel = LogSeverity.Error });
			_commands = new(new() { IgnoreExtraArgs = true, CaseSensitiveCommands = false, DefaultRunMode = RunMode.Async });

			await _client.LoginAsync(TokenType.Bot, GetToken());
			await _client.StartAsync();
			await _client.SetGameAsync("your roles", null, ActivityType.Watching);
			_client.Ready += OnReadyAsync;

			try
			{
				await Task.Delay(-1, _source.Token);
			}
			catch (TaskCanceledException)
			{
				await _client.StopAsync();
				Thread.Sleep(1000);
				await _client.LogoutAsync();
			}
			finally
			{
				_source.Dispose();
			}
		}

		private static string GetToken()
		{
			string tokenPath = Path.Combine(AppContext.BaseDirectory, "Data", "Token.txt");
			return File.ReadAllText(tokenPath);
		}

		private static async Task OnReadyAsync()
		{
			_client.Ready -= OnReadyAsync;

			IHost host = Host.CreateDefaultBuilder()
				.ConfigureServices(services =>
					services.AddSingleton(_client)
						.AddSingleton(_commands)
						.AddSingleton<IConfig, Config>()
						.AddSingleton<MessageHandlerService>()
						.AddSingleton<IDatabaseHelper, DatabaseHelper>()
						.AddSingleton<UpdateRolesHelper>()
						.AddSingleton<IDiscordHelper, DiscordHelper>()
						.AddSingleton<UserService>()
						.AddSingleton<IIOService, IOService>()
						.AddSingleton<IWebService, WebService>()
						.AddSingleton<LoggingService>()
						.AddSingleton<WelcomeMessage>()
						.AddHostedService<DdNewsPostService>())
				.Build();

			host.Services.GetRequiredService<MessageHandlerService>();
			host.Services.GetRequiredService<IDatabaseHelper>();
			host.Services.GetRequiredService<WelcomeMessage>();
			LoggingService loggingService = host.Services.GetRequiredService<LoggingService>();
			_client.Log += loggingService.LogAsync;
			_commands.Log += loggingService.LogAsync;

			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), host.Services);
			Task.Run(async () => await host.RunAsync(_source.Token), _source.Token);
		}

		public static void StopBot()
		{
			_source.Cancel();
		}
	}
}
