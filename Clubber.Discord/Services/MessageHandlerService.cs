using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Clubber.Discord.Services;

public class MessageHandlerService
{
	private readonly AppConfig _config;
	private readonly ClubberDiscordClient _client;
	private readonly CommandService _commands;
	private readonly IServiceProvider _services;

	public MessageHandlerService(
		IOptions<AppConfig> config,
		ClubberDiscordClient client,
		CommandService commands,
		IServiceProvider services)
	{
		_config = config.Value;
		_client = client;
		_commands = commands;
		_services = services;

		client.Ready += () =>
		{
			client.MessageReceived += message => Task.Run(() => OnMessageReceivedAsync(message));
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
}
