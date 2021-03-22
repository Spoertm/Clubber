using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

namespace Clubber
{
	public static class Program
	{
		private static DiscordSocketClient _client = null!;
		private static CommandService _commands = null!;
		private static IServiceProvider _services = null!;

		private static void Main() => RunBotAsync().GetAwaiter().GetResult();

		public static async Task RunBotAsync()
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

			_client = new DiscordSocketClient(new DiscordSocketConfig() { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true, LogLevel = LogSeverity.Error });
			_commands = new CommandService(new CommandServiceConfig() { IgnoreExtraArgs = true, CaseSensitiveCommands = false, DefaultRunMode = RunMode.Async });

			await _client.LoginAsync(TokenType.Bot, Constants.Token);
			await _client.StartAsync();
			await _client.SetGameAsync("your roles", null, ActivityType.Watching);

			_client.Ready += OnReadyAsync;

			await Task.Delay(-1);
		}

		private static async Task OnReadyAsync()
		{
			_client.Ready -= OnReadyAsync;

			_services = new ServiceCollection()
				.AddSingleton(_client)
				.AddSingleton(_commands)
				.AddSingleton<LoggingService>()
				.AddSingleton<MessageHandlerService>()
				.AddSingleton<IOService>()
				.AddSingleton<DatabaseHelper>()
				.AddSingleton<UpdateRolesHelper>()
				.AddSingleton<WebService>()
				.BuildServiceProvider();

			ActivatorUtilities.GetServiceOrCreateInstance<LoggingService>(_services);
			ActivatorUtilities.GetServiceOrCreateInstance<MessageHandlerService>(_services);

			IOService iOService = _services.GetRequiredService<IOService>();
			await iOService.GetDatabaseFileIntoFolder();

			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
		}

		public static async Task StopBot()
		{
			await _client.StopAsync();
			System.Threading.Thread.Sleep(1000);
			await _client.LogoutAsync();
			Environment.Exit(0);
		}
	}
}
