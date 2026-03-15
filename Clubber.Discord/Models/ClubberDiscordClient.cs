using Clubber.Discord.Logging;
using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Clubber.Discord.Models;

public sealed class ClubberDiscordClient : DiscordSocketClient
{
	private readonly AppConfig _config;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly InteractionService _interactions;

	private static readonly DiscordSocketConfig _socketConfig = new()
	{
		LogLevel = LogSeverity.Warning,
		AlwaysDownloadUsers = true,
		GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers) &
						 ~GatewayIntents.GuildInvites &
						 ~GatewayIntents.GuildScheduledEvents,
	};

	public ClubberDiscordClient(IOptions<AppConfig> options, IServiceScopeFactory scopeFactory) : base(_socketConfig)
	{
		_scopeFactory = scopeFactory;
		_config = options.Value;

		_interactions = new InteractionService(this, new InteractionServiceConfig
		{
			DefaultRunMode = RunMode.Sync,
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

		using (IServiceScope scope = _scopeFactory.CreateScope())
		{
			await _interactions.AddModulesAsync(Assembly.GetAssembly(typeof(ClubberDiscordClient)), scope.ServiceProvider);
		}

		Ready += async () =>
		{
			await _interactions.RegisterCommandsGloballyAsync(deleteMissing: true);
			Serilog.Log.Information("Slash commands registered globally");

			using IServiceScope scope = _scopeFactory.CreateScope();
			TextCommandHandler textCommandHandler = scope.ServiceProvider.GetRequiredService<TextCommandHandler>();
			await textCommandHandler.InstallCommandsAsync();
		};

		InteractionCreated += async (interaction) =>
		{
			using IServiceScope scope = _scopeFactory.CreateScope();
			SocketInteractionContext context = new(this, interaction);
			IResult result = await _interactions.ExecuteCommandAsync(context, scope.ServiceProvider);

			if (!result.IsSuccess)
			{
				Serilog.Log.Error("Slash command execution failed: {Error}", result.ErrorReason);
			}
		};
	}

	public InteractionService GetInteractionService() => _interactions;
}
