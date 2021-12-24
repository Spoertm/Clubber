using Clubber.Helpers;
using Clubber.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Services
{
	public class LoggingService
	{
		public LoggingService(DiscordSocketClient client, CommandService commands)
		{
			client.Log += LogAsync;
			commands.Log += LogAsync;
			Directory.CreateDirectory(LogDirectory);
		}

		private string LogDirectory => Path.Combine(AppContext.BaseDirectory, "Logs");
		private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

		public async Task LogAsync(LogMessage logMessage)
		{
			string logText = $"{DateTime.Now:hh:mm:ss} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Exception?.ToString() ?? logMessage.Message}";
			await File.AppendAllTextAsync(LogFile, $"{logText}\n\n");

			CommandException? commandException = logMessage.Exception as CommandException;
			if (commandException is not null)
			{
				await commandException.Context.Channel.SendMessageAsync("Catastrophic error occured.");
				if (commandException.InnerException is CustomException customException)
					await commandException.Context.Channel.SendMessageAsync(customException.Message);
			}

			Embed exceptionEmbed = EmbedHelper.Exception(logMessage, commandException?.Context?.Message);
			await DiscordHelper.LogExceptionEmbed(exceptionEmbed);
		}
	}
}
