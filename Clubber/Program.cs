using Clubber.Helpers;
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
	public class Program
	{
		private DiscordSocketClient _client = null!;
		private CommandService _commands = null!;
		private IServiceProvider _services = null!;
		private static string LogDirectory => Path.Combine(AppContext.BaseDirectory, "Logs");
		private static string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

		private static void Main() => new Program().RunBotAsync().GetAwaiter().GetResult();

		public async Task RunBotAsync()
		{
			_client = new DiscordSocketClient();
			_commands = new CommandService();
			_services = new ServiceCollection()
				.AddSingleton(_client)
				.AddSingleton(_commands)
				.AddSingleton<DatabaseHelper>()
				.BuildServiceProvider();

			_client.Log += LogAsync;
			_commands.Log += LogAsync;

			await _client.LoginAsync(TokenType.Bot, Constants.Token);
			await _client.StartAsync();
			await _client.SetGameAsync("your roles", null, ActivityType.Watching);

			_client.Ready += RegisterCommandsAsync;

			await Task.Delay(-1);
		}

		private async Task RegisterCommandsAsync()
		{
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

			_client.MessageReceived += MessageRecievedAsync;
		}

		private Task LogAsync(LogMessage msg)
		{
			if (!Directory.Exists(LogDirectory))
				Directory.CreateDirectory(LogDirectory);

			if (!File.Exists(LogFile))
				File.Create(LogFile).Dispose();

			string logText = $"{DateTime.UtcNow:hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}\n\n";
			File.AppendAllText(LogFile, logText);

			return Task.CompletedTask;
		}

		private async Task MessageRecievedAsync(SocketMessage msg)
		{
			if (msg is not SocketUserMessage message || message.Author.IsBot)
				return;

			int argumentPos = 0;
			if (message.HasStringPrefix(Constants.Prefix, ref argumentPos) || message.HasMentionPrefix(_client.CurrentUser, ref argumentPos))
			{
				SocketCommandContext context = new SocketCommandContext(_client, message);
				IResult result = await _commands.ExecuteAsync(context, argumentPos, _services);
				if (!result.IsSuccess && result.Error.HasValue)
				{
					if (result.Error.Value == CommandError.UnknownCommand)
						await msg.AddReactionAsync(new Emoji("❔"));
					else
						await context.Channel.SendMessageAsync(result.ErrorReason);
				}
			}
		}
	}
}
