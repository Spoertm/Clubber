using Clubber.Domain.Models.Exceptions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System.Diagnostics;

namespace Clubber.Domain.Services;

public class MessageHandlerService
{
	private readonly DiscordSocketClient _client;
	private readonly CommandService _commands;
	private readonly IServiceProvider _services;
	private readonly string _prefix;

	public MessageHandlerService(IConfiguration config, DiscordSocketClient client, CommandService commands, IServiceProvider services)
	{
		_client = client;
		_commands = commands;
		_services = services;

		_prefix = config["Prefix"] ?? throw new ConfigurationMissingException("Prefix");

		_client.Log += OnLog;
		_commands.Log += OnLog;

		_client.Ready += () =>
		{
			_client.MessageReceived += message => Task.Run(() => OnMessageRecievedAsync(message));
			return Task.CompletedTask;
		};
	}

	private async Task OnMessageRecievedAsync(SocketMessage msg)
	{
		if (msg is not SocketUserMessage { Source: MessageSource.User } message)
			return;

		int argumentPos = 0;
		if (!message.HasStringPrefix(_prefix, ref argumentPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argumentPos))
			return;

		SocketCommandContext context = new(_client, message);
		IResult result = await _commands.ExecuteAsync(context, argumentPos, _services);
		if (!result.IsSuccess && result.Error.HasValue)
		{
			if (result.Error.Value == CommandError.UnknownCommand)
				await message.AddReactionAsync(new Emoji("❔"));
			else
				await context.Channel.SendMessageAsync(result.ErrorReason);
		}
	}

	private static async Task OnLog(LogMessage logMessage)
	{
		if (logMessage.Exception is CommandException commandException)
		{
			await commandException.Context.Channel.SendMessageAsync("Catastrophic error occured.");
			if (commandException.InnerException is ClubberException customException)
				await commandException.Context.Channel.SendMessageAsync(customException.Message);
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

		Log.Logger.Write(logLevel, logMessage.Exception, "Source: {LogMsgSrc}\n{Msg}", logMessage.Source, logMessage.Message);
	}
}
