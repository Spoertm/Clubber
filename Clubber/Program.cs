using Clubber.Database;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
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

		private static string LogDirectory => Path.Combine(AppContext.BaseDirectory, "Logs");
		private static string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

		private static void Main() => RunBotAsync().GetAwaiter().GetResult();

		public static async Task RunBotAsync()
		{
			_client = new DiscordSocketClient(new DiscordSocketConfig() { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true });
			_commands = new CommandService(new CommandServiceConfig() { IgnoreExtraArgs = true, CaseSensitiveCommands = false, DefaultRunMode = RunMode.Async });
			_services = new ServiceCollection()
				.AddSingleton(_client)
				.AddSingleton(_commands)
				.BuildServiceProvider();

			await _client.LoginAsync(TokenType.Bot, Constants.Token);
			await _client.StartAsync();
			await _client.SetGameAsync("your roles", null, ActivityType.Watching);

			_client.Ready += RegisterCommandsAndLogAsync;

			await Task.Delay(-1);
		}

		private static async Task RegisterCommandsAndLogAsync()
		{
			_client.Ready -= RegisterCommandsAndLogAsync;

			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

			_client.MessageReceived += MessageRecievedAsync;
			_client.Log += LogAsync;
			_commands.Log += LogAsync;
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
			_ = await clubberExceptionsChannel!.SendMessageAsync(Format.Code(logText));
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
						await msg.AddReactionAsync(new Emoji("❔"));
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
