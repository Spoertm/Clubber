using Clubber.Domain.Models.Exceptions;
using Discord;
using Discord.Commands;
using Serilog.Events;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Clubber.Discord.Logging;

public static class DiscordLogHandler
{
	/// <summary>
	/// Maximum message length for Discord messages
	/// </summary>
	private const int _maxMessageLength = 2000;

	/// <summary>
	/// Handles logging Discord.Net messages to both console and channel when needed
	/// </summary>
	public static async Task Log(LogMessage logMessage)
	{
		// Handle command exceptions by sending error messages to the channel
		if (logMessage.Exception is CommandException commandException)
		{
			StringBuilder messageBuilder = new("### Error encountered:");

			// Get all useful exception details, not just ClubberException
			string exceptionDetails = GetExceptionDetails(commandException);
			if (!string.IsNullOrEmpty(exceptionDetails))
			{
				messageBuilder.Append($"\n```\n{exceptionDetails}\n```");
			}

			// Truncate if necessary to fit Discord's message limit
			string message = messageBuilder.ToString();
			if (message.Length > DiscordConfig.MaxMessageSize)
			{
				message = message[..(_maxMessageLength - 1)] + "…";
			}

			try
			{
				await commandException.Context.Channel.SendMessageAsync(message);
			}
			catch (Exception ex)
			{
				// Fallback if sending to channel fails
				Serilog.Log.Error(ex, "Failed to send error message to Discord channel");
			}
		}

		// Map Discord.Net log severity to Serilog levels
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

		// Log to Serilog
		Serilog.Log.Write(
			logLevel,
			logMessage.Exception,
			"Source: {LogMsgSrc}\n{Msg}",
			logMessage.Source,
			logMessage.Message
		);
	}

	/// <summary>
	/// Extracts a detailed error message from exceptions, including inner exceptions
	/// </summary>
	private static string GetExceptionDetails(Exception ex)
	{
		StringBuilder builder = new();

		// Check for ClubberException specifically
		if (TryGetInnerClubberException(ex, out ClubberException? clubberException))
		{
			builder.AppendLine($"ClubberException: {clubberException.Message}");
		}

		// Also include all exception details in a hierarchy
		AppendExceptionDetails(builder, ex, "");

		return builder.ToString();
	}

	/// <summary>
	/// Recursively appends exception details to the string builder
	/// </summary>
	private static void AppendExceptionDetails(StringBuilder builder, Exception? ex, string indent)
	{
		while (true)
		{
			if (ex == null)
				return;

			// Add this exception's type and message
			builder.AppendLine($"{indent}{ex.GetType().Name}: {ex.Message}");

			// Add important data from the exception
			if (ex.Data.Count > 0)
			{
				builder.AppendLine($"{indent}Data:");
				foreach (DictionaryEntry entry in ex.Data)
				{
					builder.AppendLine($"{indent}  {entry.Key}: {entry.Value}");
				}
			}

			// If there's an inner exception, recurse with indentation
			if (ex.InnerException != null)
			{
				ex = ex.InnerException;
				indent += "  ";
				continue;
			}

			break;
		}
	}

	/// <summary>
	/// Searches the exception hierarchy for a ClubberException
	/// </summary>
	private static bool TryGetInnerClubberException(Exception? ex, [NotNullWhen(true)] out ClubberException? clubberException)
	{
		// First check if the exception itself is a ClubberException
		if (ex is ClubberException cEx)
		{
			clubberException = cEx;
			return true;
		}

		// Then check all inner exceptions, not just the immediate one
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
