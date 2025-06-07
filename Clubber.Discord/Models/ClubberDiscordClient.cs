using Clubber.Discord.Logging;
using Clubber.Discord.Modules;
using Clubber.Domain.Configuration;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Clubber.Discord.Models;

public sealed class ClubberDiscordClient : DiscordSocketClient
{
	private readonly AppConfig _config;
	private readonly IServiceProvider _services;
	private readonly InteractionService _interactions;

	private static readonly DiscordSocketConfig _socketConfig = new()
	{
		LogLevel = LogSeverity.Warning,
		AlwaysDownloadUsers = true,
		GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers) &
		                 ~GatewayIntents.GuildInvites &
		                 ~GatewayIntents.GuildScheduledEvents,
	};

	public ClubberDiscordClient(IOptions<AppConfig> options, IServiceProvider services) : base(_socketConfig)
	{
		_services = services;
		_config = options.Value;

		_interactions = new InteractionService(this, new InteractionServiceConfig
		{
			DefaultRunMode = RunMode.Async,
		});

		Log += DiscordLogHandler.Log;
		_interactions.Log += DiscordLogHandler.Log;
	}

	public async Task InitAsync()
	{
		Serilog.Log.Debug("Initiating {Client}", nameof(ClubberDiscordClient));
		await LoginAsync(TokenType.Bot, _config.BotToken);
		await StartAsync();
		await SetGameAsync("your roles", null, ActivityType.Watching);

		await _interactions.AddModulesAsync(Assembly.GetAssembly(typeof(ClubberDiscordClient)), _services);

		Ready += async () =>
		{
			await _interactions.RegisterCommandsGloballyAsync();
			Serilog.Log.Information("Slash commands registered globally");
		};

		InteractionCreated += async (interaction) =>
		{
			SocketInteractionContext context = new(this, interaction);
			IResult result = await _interactions.ExecuteCommandAsync(context, _services);

			if (!result.IsSuccess)
			{
				Serilog.Log.Error("Slash command execution failed: {Error}", result.ErrorReason);
			}
		};
	}

	public InteractionService GetInteractionService() => _interactions;
}
