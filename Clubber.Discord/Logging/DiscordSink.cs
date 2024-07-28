using Clubber.Domain.Extensions;
using Discord;
using Discord.Webhook;
using Serilog.Core;
using Serilog.Events;

namespace Clubber.Discord.Logging;

public class DiscordSink : ILogEventSink
{
	private readonly LogEventLevel _minimumLogLevel;
	private readonly DiscordWebhookClient _webHook;

	public DiscordSink(ulong webhookId, string webhookToken, LogEventLevel minimumLogLevel)
	{
		_webHook = new(webhookId, webhookToken);
		_minimumLogLevel = minimumLogLevel;
	}

	public void Emit(LogEvent logEvent)
	{
		if (logEvent.Level < _minimumLogLevel)
			return;

		EmbedBuilder embedBuilder = new();
		try
		{
			string logMessage = logEvent.RenderMessage();
			SpecifyEmbedLevel(logEvent.Level, logMessage, embedBuilder);

			if (logEvent.Exception is not null)
			{
				embedBuilder.WithTitle(logEvent.Exception.Message.Truncate(256));
				string? stackTrace = logEvent.Exception.StackTrace;
				embedBuilder.WithDescription($"**StackTrace:**\n{stackTrace?.Truncate(2030) ?? "NaN"}");
				embedBuilder.AddField("Type:", nameof(logEvent.Exception), true);
				embedBuilder.AddField("Exception message:", logEvent.Exception.Message.Truncate(1024), true);
			}

			Embed embed = embedBuilder.Build();
			_webHook.SendMessageAsync(embeds: [embed]).GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			_webHook.SendMessageAsync($"Something went sideways when trying to log through Discord.\n{ex}").GetAwaiter().GetResult();
		}
	}

	private static void SpecifyEmbedLevel(LogEventLevel level, string message, EmbedBuilder embedBuilder)
	{
		(embedBuilder.Color, embedBuilder.Description) = level switch
		{
			LogEventLevel.Error or LogEventLevel.Fatal => (Color.Red, string.Empty),
			LogEventLevel.Warning                      => (Color.Gold, message),
			LogEventLevel.Debug                        => (Color.Purple, message),
			_                                          => (Color.Blue, message),
		};
	}
}
