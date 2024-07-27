﻿using Clubber.Domain.Configuration;
using Clubber.Domain.Extensions;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Diagnostics;

namespace Clubber.Discord;

public class MessageHandlerService
{
	private readonly AppConfig _config;
	private readonly DiscordSocketClient _client;
	private readonly CommandService _commands;
	private readonly IServiceProvider _services;
	private readonly IDiscordHelper _discordHelper;
	private readonly IWebService _webService;
	private readonly RegistrationTracker _registrationTracker;

	public MessageHandlerService(
		IOptions<AppConfig> config,
		DiscordSocketClient client,
		CommandService commands,
		IServiceProvider services,
		InteractionHandler interactionHandler,
		IDiscordHelper discordHelper,
		IWebService webService,
		RegistrationTracker registrationTracker)
	{
		_config = config.Value;
		_client = client;
		_commands = commands;
		_services = services;
		_discordHelper = discordHelper;
		_webService = webService;
		_registrationTracker = registrationTracker;

		_client.Log += OnLog;
		_client.ButtonExecuted += interactionHandler.OnButtonExecuted;
		_commands.Log += OnLog;

		_client.MessageReceived += message => Task.Run(() => OnMessageReceivedAsync(message));
		_client.MessageReceived += message =>
		{
			if (message is SocketUserMessage { Source: MessageSource.User } socketUserMsg && message.Channel.Id == _config.RegisterChannelId)
			{
				return Task.Run(() => ExecuteRegistrationProcedure(socketUserMsg));
			}

			return Task.CompletedTask;
		};
	}

	private async Task OnMessageReceivedAsync(SocketMessage msg)
	{
		if (msg is not SocketUserMessage { Source: MessageSource.User } message)
			return;

		int argumentPos = 0;
		if (!message.HasStringPrefix(_config.Prefix, ref argumentPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argumentPos))
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

	private async Task ExecuteRegistrationProcedure(SocketUserMessage message)
	{
		if (message.Channel.Id != _config.RegisterChannelId)
		{
			return;
		}

		if (_registrationTracker.UserIsFlagged(message.Author.Id))
		{
			await message.ReplyAsync(embed: new EmbedBuilder().WithDescription("ℹ️ You've already provided an ID. Mods will register you soon.").Build());
			return;
		}

		EmbedBuilder eb = new();
		ComponentBuilder cb = new();

		// User specified an ID
		if (message.Content.FindFirstInt() is var foundId and > 0)
		{
			EntryResponse lbPlayerInfo = (await _webService.GetLbPlayers([(uint)foundId]))[0];
			Embed fullstatsEmbed = EmbedHelper.FullStats(lbPlayerInfo, null, null);

			eb.WithDescription
			($"""
			## Register {message.Author.Mention} with ID `{foundId}`?

			### Info about ID {foundId} [from ddinfo](https://devildaggers.info/leaderboard/player/{foundId}):
			{fullstatsEmbed.Description}
			""");

			// Refer to RegistrationContext.Parse in InteractionHandler
			string buttonId = $"register:{message.Author.Id}:{foundId}:{message.Id}";

			cb.WithButton("Register", buttonId, ButtonStyle.Success);
			cb.WithButton("Deny", "deny_button", ButtonStyle.Danger);
		}
		// User specified "no score"
		else if (message.Content.Contains("no score", StringComparison.OrdinalIgnoreCase))
		{
			eb.WithDescription($"## Give {message.Author.Mention} {MentionUtils.MentionRole(_config.NoScoreRoleId)} role?");

			string buttonId = $"register:{message.Author.Id}:-1:{message.Id}";

			cb.WithButton("Confirm", buttonId, ButtonStyle.Success);
			cb.WithButton("Deny", "deny_button", ButtonStyle.Danger);
		}
		else
		{
			return;
		}

		_registrationTracker.FlagUser(message.Author.Id);

		SocketTextChannel modsChannel = _discordHelper.GetTextChannel(_config.ModsChannelId);
		await modsChannel.SendMessageAsync(embed: eb.Build(), components: cb.Build());

		const string notifMessage = "ℹ️ I've notified the mods. You'll be registered soon.";
		await message.ReplyAsync(embed: new EmbedBuilder().WithDescription(notifMessage).Build(), allowedMentions: AllowedMentions.None);
	}

	private static async Task OnLog(LogMessage logMessage)
	{
		if (logMessage.Exception is CommandException commandException)
		{
			string message = "### Catastrophic error occured.";
			if (commandException.InnerException is ClubberException customException)
				message += $"\n{customException.Message}";

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

		Log.Logger.Write(logLevel, logMessage.Exception, "Source: {LogMsgSrc}\n{Msg}", logMessage.Source, logMessage.Message);
	}
}
