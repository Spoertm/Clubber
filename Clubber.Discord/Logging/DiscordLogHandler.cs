using Clubber.Domain.Models.Exceptions;
using Discord;
using Discord.Commands;
using Serilog.Events;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Clubber.Discord.Logging;

public static class DiscordLogHandler
{
	public static async Task Log(LogMessage logMessage)
	{
		if (logMessage.Exception is CommandException commandException)
		{
			string message = "### Catastrophic error occured.";
			if (TryGetInnerClubberException(commandException, out ClubberException? clubberException))
			{
				message += $"\n{clubberException.Message}";
			}

			await commandException.Context.Channel.SendMessageAsync(message);
		}

		LogEventLevel logLevel = logMessage.Severity switch
		{
			LogSeverity.Critical => LogEventLevel.Fatal,
			LogSeverity.Error    => LogEventLevel.Error,
			LogSeverity.Warning  => LogEventLevel.Warning,
			LogSeverity.Info     => LogEventLevel.Information,
			LogSeverity.Verbose  => LogEventLevel.Verbose,
			LogSeverity.Debug    => LogEventLevel.Debug,
			_                    => throw new UnreachableException($"Encountered unreachable {nameof(LogSeverity)} with value {logMessage.Severity}."),
		};

		Serilog.Log.Logger.Write(logLevel, logMessage.Exception, "Source: {LogMsgSrc}\n{Msg}", logMessage.Source, logMessage.Message);
	}

	private static bool TryGetInnerClubberException(Exception? ex, [NotNullWhen(true)] out ClubberException? clubberException)
	{
		while (ex?.InnerException is { } innerEx)
		{
			if (innerEx is ClubberException clubberEx)
			{
				clubberException = clubberEx;
				return true;
			}

			ex = innerEx;
		}

		clubberException = null;
		return false;
	}
}
