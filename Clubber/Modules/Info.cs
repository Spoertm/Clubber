﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	public class Info : AbstractModule<SocketCommandContext>
	{
		private readonly CommandService _service;
		private static DiscordSocketClient _client = null!;

		public Info(CommandService service, DiscordSocketClient client)
		{
			_service = service;
			_client = client;
		}

		[Command("stopbot")]
		[RequireOwner]
		public async Task StopBot()
		{
			await ReplyAsync("Exiting...");
			await Program.StopBot();
		}

		[Command("whyareyou")]
		[Summary("Describes what the bot does.")]
		public async Task WhyAreYou() => await InlineReplayAsync(Constants.WhyAreYou);

		[Command("help")]
		[Summary("Get a list of commands, or info regarding a specific command.")]
		public async Task Help([Remainder] string command)
		{
			SearchResult result = _service.Search(Context, command);
			if (await IsError(!result.IsSuccess, $"The command `{command}` doesn't exist."))
				return;

			PreconditionResult preCondCheck = await result.Commands[0].Command.CheckPreconditionsAsync(Context);
			if (await IsError(!preCondCheck.IsSuccess, $"The command `{command}` couldn't be executed.\nReason: " + preCondCheck.ErrorReason))
				return;

			string aliases = string.Empty;
			EmbedBuilder embedBuilder = new();

			CommandInfo? cmd = result.Commands[0].Command;
			if (cmd.Module.Group == null)
			{
				if (cmd.Aliases.Count > 1)
					aliases = $"\nAliases: {string.Join(", ", cmd.Aliases)}";
			}
			else
			{
				aliases = $"\nAliases: {string.Join(", ", cmd.Module.Aliases)}";
			}

			if (result.Commands[0].Command.Module.Group == null)
				embedBuilder.Title = $"{string.Join("\n", result.Commands.Where(c => c.CheckPreconditionsAsync(Context).Result.IsSuccess).Select(c => GetCommandAndParameterString(c.Command)))}\n{aliases}";
			else
				embedBuilder.Title = $"{string.Join("\n", result.Commands[0].Command.Module.Commands.Where(c => c.CheckPreconditionsAsync(Context).Result.IsSuccess).Select(c => GetCommandAndParameterString(c)))}\n{aliases}";
			embedBuilder.Description = cmd.Summary ?? cmd.Module.Summary;

			if (result.Commands.Any(c => c.Command.Parameters.Count > 0))
				embedBuilder.Footer = new EmbedFooterBuilder { Text = "[]: Required⠀⠀(): Optional\nText within \" \" will be counted as one argument." };

			await ReplyAsync(null, false, embedBuilder.Build(), null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}

		[Command("help")]
		[Summary("Get a list of commands, or info regarding a specific command.")]
		public async Task Help()
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("List of commands")
				.WithDescription($"To check for role updates do `{Constants.Prefix}pb`\nTo get stats do `{Constants.Prefix}me`\n\n")
				.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
				.WithFooter("Mentioning the bot works as well as using the prefix.\nUse help <command> to get more info about a command.");

			foreach (IGrouping<string, CommandInfo>? group in _service.Commands.GroupBy(x => x.Module.Name))
			{
				string groupCommands = string.Join(", ", group
					.Where(cmd => cmd.CheckPreconditionsAsync(Context).Result.IsSuccess)
					.Select(x => Format.Code(x.Aliases[0]))
					.Distinct());

				if (!string.IsNullOrEmpty(groupCommands))
					embed.AddField(group.Key, groupCommands);
			}

			await ReplyAsync(null, false, embed.Build(), null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}

		/// <summary>
		/// Returns the command and its params in the format: commandName [requiredParam] (optionalParam).
		/// </summary>
		private static string GetCommandAndParameterString(CommandInfo cmd)
			=> $"{cmd.Aliases[0]} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**({p.Name})**" : $"**({p.Name} = {p.DefaultValue})**" : $"**[{p.Name}]**"))}";

		public static async Task BackupDbFile(System.IO.Stream stream, string fileName)
		{
			SocketTextChannel? backupChannel = _client.GetChannel(Constants.DatabaseBackupChannel) as SocketTextChannel;
			await backupChannel!.SendFileAsync(stream, fileName, string.Empty);
		}
	}
}
