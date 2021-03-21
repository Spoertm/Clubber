using Clubber.Database;
using Clubber.Helpers;
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
		private static SocketCommandContext _context = null!;
		public static string DatabaseDirectory => Path.Combine(AppContext.BaseDirectory, "Database");
		private static string LogDirectory => Path.Combine(AppContext.BaseDirectory, "Logs");
		private static string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

		private static void Main() => RunBotAsync().GetAwaiter().GetResult();

		public static async Task RunBotAsync()
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

			_client = new DiscordSocketClient(new DiscordSocketConfig() { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true, LogLevel = LogSeverity.Error });
			_commands = new CommandService(new CommandServiceConfig() { IgnoreExtraArgs = true, CaseSensitiveCommands = false, DefaultRunMode = RunMode.Async });
			_services = new ServiceCollection()
				.AddSingleton(_client)
				.AddSingleton(_commands)
				.BuildServiceProvider();

			await _client.LoginAsync(TokenType.Bot, Constants.Token);
			await _client.StartAsync();
			await _client.SetGameAsync("your roles", null, ActivityType.Watching);

			_client.Ready += OnReadyAsync;

			await Task.Delay(-1);
		}

		private static async Task OnReadyAsync()
		{
			_client.Ready -= OnReadyAsync;

			await GetDatabaseFileIntoFolder((_client.GetChannel(Constants.DatabaseBackupChannel) as SocketTextChannel)!, (_client.GetChannel(Constants.ClubberExceptionsChannel) as SocketTextChannel)!);

			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

			_client.MessageReceived += MessageRecievedAsync;
			_client.Log += LogAsync;
			_commands.Log += LogAsync;
		}

		public static async Task GetDatabaseFileIntoFolder(SocketTextChannel backupChannel, SocketTextChannel exceptionsChannel)
		{
			try
			{
				IAttachment? latestAttachment = (await backupChannel!.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault()!.Attachments.First();

				if (Directory.Exists(DatabaseDirectory))
					Directory.Delete(DatabaseDirectory, true);

				Directory.CreateDirectory(DatabaseDirectory);
				string filePath = Path.Combine(DatabaseDirectory, latestAttachment.Filename);

				using HttpClient client = new();
				string databaseJson = await client.GetStringAsync(latestAttachment.Url);
				File.WriteAllText(filePath, databaseJson);
			}
			catch (Exception ex)
			{
				await exceptionsChannel!.SendMessageAsync($"`{DateTime.Now:hh:mm:ss} [Critical] No database found. Exiting.`\n" + ex.Message);
				await StopBot();
			}
		}

		private static async Task LogAsync(LogMessage msg)
		{
			if (!Directory.Exists(LogDirectory))
				Directory.CreateDirectory(LogDirectory);

			if (!File.Exists(LogFile))
				File.Create(LogFile).Dispose();

			if (msg.Exception?.InnerException is CustomException customException)
				await _context.Channel.SendMessageAsync(customException.Message);

			string logText = $"{DateTime.Now:hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
			File.AppendAllText(LogFile, $"{logText}\n\n");

			SocketTextChannel? clubberExceptionsChannel = _client.GetChannel(Constants.ClubberExceptionsChannel) as SocketTextChannel;
			Embed exceptionEmbed = EmbedHelper.Exception(msg, _context.Message);
			_ = await clubberExceptionsChannel!.SendMessageAsync(null, false, exceptionEmbed);
		}

		private static async Task MessageRecievedAsync(SocketMessage msg)
		{
			if (msg is not SocketUserMessage message || message.Author.IsBot)
				return;

			int argumentPos = 0;
			if (message.HasStringPrefix(Constants.Prefix, ref argumentPos) || message.HasMentionPrefix(_client.CurrentUser, ref argumentPos))
			{
				_context = new SocketCommandContext(_client, message);
				IResult result = await _commands.ExecuteAsync(_context, argumentPos, _services);
				if (!result.IsSuccess && result.Error.HasValue)
				{
					if (result.Error.Value == CommandError.UnknownCommand)
						await message.AddReactionAsync(new Emoji("❔"));
					else
						await _context.Channel.SendMessageAsync(result.ErrorReason);
				}
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
