using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;
		private readonly IConfigurationRoot config;
		private readonly IServiceProvider provider;

		// DiscordSocketClient, CommandService, IConfigurationRoot, and IServiceProvider are injected automatically from the IServiceProvider
		public CommandHandler(DiscordSocketClient _discord, CommandService _commands, IConfigurationRoot _config, IServiceProvider _provider)
		{
			discord = _discord;
			commands = _commands;
			config = _config;
			provider = _provider;

			discord.MessageReceived += OnMessageReceivedAsync;
		}

		private async Task OnMessageReceivedAsync(SocketMessage s)
		{
			if (!(s is SocketUserMessage msg) || msg.Author.Id == discord.CurrentUser.Id) // Ensure the message is from a user/bot and ignore self when checking commands
				return;

			var context = new SocketCommandContext(discord, msg);     // Create the command context

			if (msg.Content == context.Client.GetUser(743431502842298368).Mention)
			{ await msg.AddReactionAsync(new Emoji("🗡")); return; }

			int argPos = 0;     // Check if the message has a valid command prefix
			if (msg.HasStringPrefix(config["prefix"], ref argPos) || msg.HasMentionPrefix(discord.CurrentUser, ref argPos))
			{
				var commandSearch = commands.Search(context, argPos);
				if (commandSearch.IsSuccess)
				{
					var command = commandSearch.Commands[0].Command;

					if (!new string[] { "help", "updateroles", "printdb", "showunregisteredusers", "stats" }.Contains(command.Name) &&
						command.Parameters.Count > 0 &&
						command.Aliases.Any(msg.Content.Substring(argPos).Equals) &&
						msg.Attachments.Count == 0)
					{ await commands.ExecuteAsync(context, $"help {msg.Content.Substring(argPos)}", provider); return; }

					var result = await commands.ExecuteAsync(context, msg.Content.Substring(argPos), provider);
					if (!result.IsSuccess) await context.Channel.SendMessageAsync(result.ErrorReason);
				}
			}
		}
	}
}