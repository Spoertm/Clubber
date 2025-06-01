using Clubber.Domain.Extensions;
using Discord;
using Discord.Webhook;
using Serilog.Core;
using Serilog.Events;
using System.Collections;
using System.Text;

namespace Clubber.Discord.Logging;

public sealed class DiscordSink : ILogEventSink
{
	private readonly LogEventLevel _minimumLogLevel;
	private readonly DiscordWebhookClient _webHook;

	// Maximum number of nested exceptions to include
	private const int _maxExceptionDepth = 5;

	public DiscordSink(ulong webhookId, string webhookToken, LogEventLevel minimumLogLevel)
	{
		_webHook = new DiscordWebhookClient(webhookId, webhookToken);
		_minimumLogLevel = minimumLogLevel;
	}

	public void Emit(LogEvent logEvent)
	{
		if (logEvent.Level < _minimumLogLevel)
			return;

		try
		{
			// Send all logs as embeds
			SendEmbedLog(logEvent);
		}
		catch (Exception ex)
		{
			// Last resort fallback if logging itself fails - this still uses an embed
			try
			{
				EmbedBuilder errorEmbed = new();
				errorEmbed.WithTitle("❌ LOGGING FAILURE");
				errorEmbed.WithColor(Color.DarkRed);
				errorEmbed.WithDescription($"Failed to log message: {ex.Message}");
				errorEmbed.WithTimestamp(DateTimeOffset.Now);

				_webHook.SendMessageAsync(embeds: [errorEmbed.Build()]).GetAwaiter().GetResult();
			}
			catch
			{
				// Nothing more we can do if this fails
			}
		}
	}

	/// <summary>
	/// Sends a log as an embed
	/// </summary>
	private void SendEmbedLog(LogEvent logEvent)
	{
		EmbedBuilder embedBuilder = new();
		string logMessage = logEvent.RenderMessage();

		// Set embed properties based on log level
		SpecifyEmbedLevel(logEvent.Level, logMessage, embedBuilder);

		// Add timestamp
		embedBuilder.WithTimestamp(DateTimeOffset.Now);

		// Handle exception information
		if (logEvent.Exception is not null)
		{
			// For critical errors, optimize the embed layout to show all relevant info
			if (logEvent.Level >= LogEventLevel.Error)
			{
				FormatCriticalErrorEmbed(embedBuilder, logEvent.Exception);
			}
			else
			{
				// For less critical errors, use standard formatting
				FormatStandardErrorEmbed(embedBuilder, logEvent.Exception);
			}
		}

		// Add any structured log properties as fields
		foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties)
		{
			// Skip very long property values or convert to string properly
			string value = property.Value.ToString();
			if (value.Length <= EmbedFieldBuilder.MaxFieldValueLength)
			{
				embedBuilder.AddField(
					property.Key.Truncate(EmbedFieldBuilder.MaxFieldNameLength),
					value.Truncate(EmbedFieldBuilder.MaxFieldValueLength),
					true);
			}
		}

		// Build and send the embed
		Embed embed = embedBuilder.Build();
		_webHook.SendMessageAsync(embeds: [embed]).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Formats an embed for critical errors to maximize information visibility
	/// </summary>
	private static void FormatCriticalErrorEmbed(EmbedBuilder embedBuilder, Exception exception)
	{
		// Set the title to the exception message
		string title = exception.Message;
		embedBuilder.WithTitle(title.Truncate(EmbedBuilder.MaxTitleLength));

		// Create a description that includes the exception hierarchy
		StringBuilder descriptionBuilder = new();

		// Start with the exception type
		descriptionBuilder.AppendLine($"**Exception Type:** {exception.GetType().Name}");

		// Add the full exception message
		descriptionBuilder.AppendLine($"**Message:** {exception.Message}");

		// Add stack trace in code block
		if (exception.StackTrace != null)
		{
			descriptionBuilder.AppendLine("**Stack Trace:**");
			descriptionBuilder.AppendLine("```");
			descriptionBuilder.AppendLine(exception.StackTrace.Truncate(1500)); // Limit to fit in description
			descriptionBuilder.AppendLine("```");
		}

		// Set the description with this content
		embedBuilder.WithDescription(descriptionBuilder.ToString().Truncate(EmbedBuilder.MaxDescriptionLength));

		// Add inner exceptions as fields
		Exception? currentEx = exception.InnerException;
		int depth = 1;

		while (currentEx != null && depth < _maxExceptionDepth)
		{
			string fieldTitle = depth == 1 ? "Inner Exception" : $"Inner Exception {depth}";

			// For the first inner exception, provide more details
			if (depth == 1)
			{
				StringBuilder innerExBuilder = new();
				innerExBuilder.AppendLine($"**Type:** {currentEx.GetType().Name}");
				innerExBuilder.AppendLine($"**Message:** {currentEx.Message}");

				// Include a snippet of stack trace for the first inner exception
				if (currentEx.StackTrace != null)
				{
					string[] stackLines = currentEx.StackTrace.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
					int lineCount = Math.Min(5, stackLines.Length); // Take up to 5 lines

					innerExBuilder.AppendLine("**Trace:**");
					for (int i = 0; i < lineCount; i++)
					{
						innerExBuilder.AppendLine(stackLines[i]);
					}

					if (lineCount < stackLines.Length)
					{
						innerExBuilder.AppendLine("...");
					}
				}

				embedBuilder.AddField(fieldTitle, innerExBuilder.ToString().Truncate(EmbedFieldBuilder.MaxFieldValueLength));
			}
			else
			{
				// For deeper exceptions, just show type and message
				embedBuilder.AddField(fieldTitle,
					$"{currentEx.GetType().Name}: {currentEx.Message}".Truncate(EmbedFieldBuilder.MaxFieldValueLength));
			}

			currentEx = currentEx.InnerException;
			depth++;
		}

		// Indicate if we truncated exceptions
		if (currentEx != null)
		{
			embedBuilder.AddField("Additional Exceptions",
				$"*There are {depth - _maxExceptionDepth} more inner exceptions*");
		}

		// Add exception data if present
		if (exception.Data.Count > 0)
		{
			StringBuilder dataBuilder = new();
			foreach (DictionaryEntry entry in exception.Data)
			{
				dataBuilder.AppendLine($"{entry.Key}: {entry.Value}");
			}

			embedBuilder.AddField("Exception Data",
				dataBuilder.ToString().Truncate(EmbedFieldBuilder.MaxFieldValueLength));
		}
	}

	/// <summary>
	/// Formats an embed for standard (non-critical) errors
	/// </summary>
	private static void FormatStandardErrorEmbed(EmbedBuilder embedBuilder, Exception exception)
	{
		// Set the title to the exception message
		string title = exception.Message;
		embedBuilder.WithTitle(title.Truncate(EmbedBuilder.MaxTitleLength));

		// Add exception type as a field
		string exceptionType = exception.GetType().FullName ?? "Unknown Exception Type";
		embedBuilder.AddField("Type:", exceptionType.Truncate(EmbedFieldBuilder.MaxFieldValueLength), true);

		// Add stack trace as description, prioritizing the most useful trace
		string? stackTrace = exception.StackTrace ?? exception.InnerException?.StackTrace;
		if (stackTrace != null)
		{
			embedBuilder.WithDescription($"**StackTrace:**\n```\n{stackTrace.Truncate(EmbedBuilder.MaxDescriptionLength - 20)}\n```");
		}

		// Handle inner exceptions - add as separate fields
		if (exception.InnerException != null)
		{
			Exception? innerEx = exception.InnerException;
			embedBuilder.AddField("Inner Exception:",
				$"{innerEx.GetType().Name}: {innerEx.Message}".Truncate(EmbedFieldBuilder.MaxFieldValueLength));

			// Add another level if available - critical for understanding many errors
			if (innerEx.InnerException != null)
			{
				Exception? innerInnerEx = innerEx.InnerException;
				embedBuilder.AddField("Root Cause:",
					$"{innerInnerEx.GetType().Name}: {innerInnerEx.Message}".Truncate(EmbedFieldBuilder.MaxFieldValueLength));
			}
		}

		// Add any exception data as fields
		if (exception.Data.Count > 0)
		{
			StringBuilder dataBuilder = new();
			foreach (DictionaryEntry entry in exception.Data)
			{
				dataBuilder.AppendLine($"{entry.Key}: {entry.Value}");
			}

			embedBuilder.AddField("Exception Data:", dataBuilder.ToString().Truncate(EmbedFieldBuilder.MaxFieldValueLength));
		}
	}

	/// <summary>
	/// Configures the embed appearance based on log level
	/// </summary>
	private static void SpecifyEmbedLevel(LogEventLevel level, string message, EmbedBuilder embedBuilder)
	{
		(embedBuilder.Color, embedBuilder.Description, string title) = level switch
		{
			LogEventLevel.Fatal   => (Color.DarkRed, string.Empty, "💥 FATAL ERROR"),
			LogEventLevel.Error   => (Color.Red, string.Empty, "❌ ERROR"),
			LogEventLevel.Warning => (Color.Gold, message, "⚠️ WARNING"),
			LogEventLevel.Debug   => (Color.Purple, message, "🔍 DEBUG"),
			LogEventLevel.Verbose => (Color.LightGrey, message, "📝 VERBOSE"),
			_                     => (Color.Blue, message, "ℹ️ INFO"),
		};

		// Only set the title if we don't have an exception (which will set its own title)
		if (string.IsNullOrEmpty(embedBuilder.Title))
		{
			embedBuilder.WithTitle(title);
		}
	}
}
