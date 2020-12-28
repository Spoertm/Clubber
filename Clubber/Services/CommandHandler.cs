using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Clubber
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private readonly IConfigurationRoot _config;
		private readonly IServiceProvider _provider;

		public CommandHandler(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
		{
			_discord = discord;
			_commands = commands;
			_config = config;
			_provider = provider;

			_discord.MessageReceived += OnMessageReceivedAsync;
		}

		private async Task OnMessageReceivedAsync(SocketMessage message)
		{
			if (message is not SocketUserMessage msg || msg.Source != MessageSource.User)
				return;

			if (msg.Content == _discord.CurrentUser.Mention)
			{
				await msg.AddReactionAsync(new Emoji("🗡"));
				return;
			}

			int argPos = 0;
			if (!(msg.HasStringPrefix(_config["prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos)))
				return;

			SocketCommandContext context = new SocketCommandContext(_discord, msg);

			IResult result = await _commands.ExecuteAsync(context, msg.Content[argPos..], _provider);

			if (!result.IsSuccess && result.Error.HasValue)
			{
				if (result.Error.Value == CommandError.UnknownCommand)
					await msg.AddReactionAsync(new Emoji("❔"));
				else
					await context.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}
}
