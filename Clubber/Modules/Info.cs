using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
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

			EmbedBuilder embedBuilder = new();
			CommandInfo cmd = result.Commands[0].Command;

			embedBuilder
				.WithAuthor(Context.Client.CurrentUser.Username, Context.Client.CurrentUser.GetAvatarUrl())
				.WithTitle(result.Commands[0].Alias)
				.WithDescription(cmd.Summary ?? cmd.Module.Summary);

			if (cmd.Aliases.Count > 1)
				embedBuilder.AddField("Aliases", string.Join('\n', result.Commands[0].Command.Aliases), true);

			IEnumerable<CommandInfo> checkedCommands;

			if (result.Commands[0].Command.Module.Group == null)
				checkedCommands = result.Commands.Where(c => c.CheckPreconditionsAsync(Context).Result.IsSuccess).Select(c => c.Command);
			else
				checkedCommands = result.Commands[0].Command.Module.Commands.Where(c => c.CheckPreconditionsAsync(Context).Result.IsSuccess);

			if (checkedCommands.Count() > 1 || checkedCommands.Any(cc => cc.Parameters.Count > 0))
			{
				embedBuilder.AddField("Overloads", string.Join('\n', checkedCommands.Select(cc => GetCommandAndParameterString(cc))), true);
				embedBuilder.AddField("Examples", string.Join('\n', checkedCommands.Select(cc => cc.Remarks)));
			}

			if (result.Commands.Any(c => c.Command.Parameters.Count > 0))
				embedBuilder.WithFooter("[]: Required⠀⠀(): Optional\nText within \" \" will be counted as one argument.");

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
			=> $"{cmd.Aliases[0]} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"({p.Name})" : $"({p.Name} = {p.DefaultValue})" : $"[{p.Name}]"))}";

		public static async Task BackupDbFile(System.IO.Stream stream, string fileName)
		{
			SocketTextChannel? backupChannel = _client.GetChannel(Constants.DatabaseBackupChannel) as SocketTextChannel;
			await backupChannel!.SendFileAsync(stream, fileName, string.Empty);
		}
	}
}
