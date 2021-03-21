using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Clubber
{
	public static class Program
	{
		private static DiscordSocketClient _client = null!;
		private static CommandService _commands = null!;
		private static IServiceProvider _services = null!;

		public static string DatabaseDirectory => Path.Combine(AppContext.BaseDirectory, "Database");

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
				.AddSingleton(new LoggingService(_client, _commands))
				.AddSingleton(new MessageHandlerService(_client, _commands, _services))
				.BuildServiceProvider();

			await GetDatabaseFileIntoFolder();

			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
		}

		public static async Task GetDatabaseFileIntoFolder()
		{
			try
			{
				SocketTextChannel backupChannel = (_client.GetChannel(Constants.DatabaseBackupChannel) as SocketTextChannel)!;
				IAttachment? latestAttachment = (await backupChannel!.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault()!.Attachments.First();

				if (Directory.Exists(DatabaseDirectory))
					Directory.Delete(DatabaseDirectory, recursive: true);

				Directory.CreateDirectory(DatabaseDirectory);
				string filePath = Path.Combine(DatabaseDirectory, latestAttachment.Filename);

				using HttpClient client = new();
				string databaseJson = await client.GetStringAsync(latestAttachment.Url);
				File.WriteAllText(filePath, databaseJson);
			}
			catch (Exception ex)
			{
				await _services.GetRequiredService<LoggingService>()
					.LogAsync(new LogMessage(LogSeverity.Critical, "Startup", "Failed to get database file into folder.", ex));
				await StopBot();
			}
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
