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
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;
		private readonly IConfigurationRoot config;
		private readonly IServiceProvider provider;

		public CommandHandler(DiscordSocketClient _discord, CommandService _commands, IConfigurationRoot _config, IServiceProvider _provider)
		{
			discord = _discord;
			commands = _commands;
			config = _config;
			provider = _provider;

			discord.MessageReceived += OnMessageReceivedAsync;
		}

		private async Task OnMessageReceivedAsync(SocketMessage message)
		{
			if (!(message is SocketUserMessage msg) || msg.Source != MessageSource.User) return;

			if (msg.Content == discord.CurrentUser.Mention)
			{
				await msg.AddReactionAsync(new Emoji("🗡"));
				return;
			}

			int argPos = 0;
			if (!(msg.HasStringPrefix(config["prefix"], ref argPos) || msg.HasMentionPrefix(discord.CurrentUser, ref argPos))) return;

			SocketCommandContext context = new SocketCommandContext(discord, msg);

			var result = await commands.ExecuteAsync(context, msg.Content.Substring(argPos), provider);

			if (!result.IsSuccess && result.Error.HasValue)
			{
				if (result.Error.Value == CommandError.UnknownCommand) await msg.AddReactionAsync(new Emoji("❔"));
				else await context.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}
}