using Clubber.Helpers;
using Clubber.Models;
using Discord;
using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public class LoggingService
	{
		public LoggingService()
		{
			Directory.CreateDirectory(LogDirectory);
		}

		private string LogDirectory => Path.Combine(AppContext.BaseDirectory, "Logs");
		private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

		public async Task LogAsync(LogMessage logMessage)
		{
			ICommandContext? context = (logMessage.Exception as CommandException)?.Context;
			if (logMessage.Exception?.InnerException is CustomException customException && context is not null)
				await context.Channel.SendMessageAsync(customException.Message);

			string logText = $"{DateTime.Now:hh:mm:ss} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Exception?.ToString() ?? logMessage.Message}";
			await File.AppendAllTextAsync(LogFile, $"{logText}\n\n");

			Embed exceptionEmbed = EmbedHelper.Exception(logMessage, context?.Message);
			await DiscordHelper.LogExceptionEmbed(exceptionEmbed);
		}
	}
}
