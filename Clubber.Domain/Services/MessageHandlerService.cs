using Clubber.Domain.Models.Exceptions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using IResult = Discord.Commands.IResult;

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
}
