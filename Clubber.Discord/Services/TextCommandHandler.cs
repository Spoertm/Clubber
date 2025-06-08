using Clubber.Discord.Logging;
using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;
using System.Reflection;

namespace Clubber.Discord.Services;

public sealed class TextCommandHandler
{
	private readonly ClubberDiscordClient _client;
	private readonly CommandService _commandService;
	private readonly IServiceProvider _services;
	private readonly AppConfig _config;

	public TextCommandHandler(
		ClubberDiscordClient client,
		CommandService commandService,
		IServiceProvider services,
		IOptions<AppConfig> config)
	{
		_client = client;
		_commandService = commandService;
		_services = services;
		_config = config.Value;

		_commandService.Log += DiscordLogHandler.Log;
		_client.MessageReceived += HandleCommandAsync;
	}

	public async Task InstallCommandsAsync()
	{
		await _commandService.AddModulesAsync(Assembly.GetAssembly(typeof(TextCommandHandler)), _services);
		Log.Information("Text commands registered");
	}

	private async Task HandleCommandAsync(SocketMessage msg)
	{
		if (msg is not SocketUserMessage { Source: MessageSource.User } message)
			return;

		int argumentPos = 0;
		if (!message.HasStringPrefix(_config.Prefix, ref argumentPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argumentPos))
			return;

		SocketCommandContext context = new(_client, message);
		IResult result = await _commandService.ExecuteAsync(context, argumentPos, _services);
		if (!result.IsSuccess && result.Error.HasValue)
		{
			if (result.Error.Value == CommandError.UnknownCommand)
			{
				await message.AddReactionAsync(new Emoji("❔"));
			}
			else
			{
				await context.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}
}
