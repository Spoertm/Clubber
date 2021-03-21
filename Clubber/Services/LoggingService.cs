﻿using Clubber.Database;
using Clubber.Helpers;
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
		private readonly SocketTextChannel _clubberExceptionsChannel;

		public LoggingService(DiscordSocketClient client, CommandService commands)
		{
			LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");

			client.Log += LogAsync;
			commands.Log += LogAsync;

			_clubberExceptionsChannel = (client.GetChannel(Constants.ClubberExceptionsChannel) as SocketTextChannel)!;
		}

		private string LogDirectory { get; }
		private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

		public async Task LogAsync(LogMessage logMessage)
		{
			if (!Directory.Exists(LogDirectory))
				Directory.CreateDirectory(LogDirectory);

			if (!File.Exists(LogFile))
				File.Create(LogFile).Dispose();

			ICommandContext? context = (logMessage.Exception as CommandException)?.Context;
			if (logMessage.Exception?.InnerException is CustomException customException && context is not null)
				await context.Channel.SendMessageAsync(customException.Message);

			string logText = $"{DateTime.Now:hh:mm:ss} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Exception?.ToString() ?? logMessage.Message}";
			File.AppendAllText(LogFile, $"{logText}\n\n");

			Embed exceptionEmbed = EmbedHelper.Exception(logMessage, context?.Message);
			_ = await _clubberExceptionsChannel.SendMessageAsync(null, false, exceptionEmbed);
		}
	}
}