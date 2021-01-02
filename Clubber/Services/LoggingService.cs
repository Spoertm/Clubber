using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Clubber
{
	public class LoggingService
	{
		// DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
		public LoggingService(DiscordSocketClient discord, CommandService commands)
		{
			LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

			discord.Log += OnLogAsync;
			commands.Log += OnLogAsync;
		}

		private string LogDirectory { get; }
		private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

		private Task OnLogAsync(LogMessage msg)
		{
			if (!Directory.Exists(LogDirectory)) // Create the log directory if it doesn't exist
				Directory.CreateDirectory(LogDirectory);
			if (!File.Exists(LogFile)) // Create today's log file if it doesn't exist
				File.Create(LogFile).Dispose();

			string logText = $"{DateTime.UtcNow:hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
			File.AppendAllText(LogFile, logText + "\n"); // Write the log text to a file

			return Console.Out.WriteLineAsync(logText); // Write the log text to the console
		}
	}
}
