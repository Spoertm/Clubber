using Clubber.Discord.Logging;
using Clubber.Discord.Modules;
using Clubber.Domain.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Clubber.Discord.Models;

public class ClubberDiscordClient : DiscordSocketClient
{
	private readonly AppConfig _config;
	private readonly CommandService _commands;
	private readonly IServiceProvider _services;
	private static readonly DiscordSocketConfig _socketConfig = new()
	{
		LogLevel = LogSeverity.Warning,
		AlwaysDownloadUsers = true,
		GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers) &
						~GatewayIntents.GuildInvites &
						~GatewayIntents.GuildScheduledEvents,
	};

	public ClubberDiscordClient(IOptions<AppConfig> options, CommandService commands, IServiceProvider services) : base(_socketConfig)
	{
		_commands = commands;
		_services = services;
		_config = options.Value;

		Log += DiscordLogHandler.Log;
		commands.Log += DiscordLogHandler.Log;
	}

	public async Task InitAsync()
	{
		Serilog.Log.Debug("Initiating {Client}", nameof(ClubberDiscordClient));
		await LoginAsync(TokenType.Bot, _config.BotToken);
		await StartAsync();
		await SetGameAsync("your roles", null, ActivityType.Watching);
		await _commands.AddModulesAsync(Assembly.GetAssembly(typeof(ExtendedModulebase<>)), _services);
	}
}
