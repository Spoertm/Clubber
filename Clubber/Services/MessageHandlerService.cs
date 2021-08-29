using Clubber.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public class MessageHandlerService
	{
		private readonly IConfig _config;
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly IServiceProvider _services;

		public MessageHandlerService(IConfig config, DiscordSocketClient client, CommandService commands, IServiceProvider services)
		{
			_config = config;
			_client = client;
			_commands = commands;
			_services = services;

			client.MessageReceived += OnMessageRecievedAsync;
		}

		private async Task OnMessageRecievedAsync(SocketMessage msg)
		{
			if (msg is not SocketUserMessage { Source: MessageSource.User } message)
				return;

			int argumentPos = 0;
			if (message.HasStringPrefix(_config.Prefix, ref argumentPos) || message.HasMentionPrefix(_client.CurrentUser, ref argumentPos))
			{
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
	}
}
