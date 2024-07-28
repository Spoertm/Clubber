using Clubber.Discord.Logging;
using Clubber.Domain.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Clubber.Discord;

public class ClubberDiscordClient : DiscordSocketClient
{
	private readonly AppConfig _config;
	private static readonly DiscordSocketConfig _socketConfig = new()
	{
		LogLevel = LogSeverity.Warning,
		AlwaysDownloadUsers = true,
		GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers) &
						~GatewayIntents.GuildInvites &
						~GatewayIntents.GuildScheduledEvents,
	};

	public ClubberDiscordClient(
		IOptions<AppConfig> options,
		CommandService commands,
		MessageHandlerService messageHandlerService,
		InteractionHandler interactionHandler,
		RegistrationRequestHandler registrationRequestHandler)
		: base(_socketConfig)
	{
		_config = options.Value;

		Log += DiscordLogHandler.Log;
		commands.Log += DiscordLogHandler.Log;
		ButtonExecuted += interactionHandler.OnButtonExecuted;

		Ready += () =>
		{
			MessageReceived += message => Task.Run(() => messageHandlerService.OnMessageReceivedAsync(message));

			MessageReceived += message =>
			{
				if (message is SocketUserMessage { Source: MessageSource.User } socketUserMsg && message.Channel.Id == _config.RegisterChannelId)
				{
					return Task.Run(() => registrationRequestHandler.Handle(socketUserMsg));
				}

				return Task.CompletedTask;
			};

			return Task.CompletedTask;
		};
	}

	public async Task InitAsync()
	{
		Serilog.Log.Debug("Initiating {Client}", nameof(ClubberDiscordClient));
		await LoginAsync(TokenType.Bot, _config.BotToken);
		await StartAsync();
		await SetGameAsync("your roles", null, ActivityType.Watching);
	}
}
