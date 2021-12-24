using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IResult = Discord.Commands.IResult;

namespace Clubber.Services
{
	public class MessageHandlerService
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly IServiceProvider _services;
		private readonly string _prefix = Environment.GetEnvironmentVariable("Prefix")!;

		public MessageHandlerService(DiscordSocketClient client, CommandService commands, IServiceProvider services)
		{
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
}
