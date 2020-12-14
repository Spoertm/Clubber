using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Info"), Group("help")]
	[Summary("Get a list of commands, or info regarding a specific command.")]
	public class HelpModule : AbstractModule<SocketCommandContext>
	{
		private readonly CommandService service;
		private readonly IConfigurationRoot config;

		public HelpModule(CommandService _service, IConfigurationRoot _config)
		{
			service = _service;
			config = _config;
		}

		[Command]
		public async Task Help([Remainder] string command)
		{
			var result = service.Search(Context, command);
			var preCondCheck = result.IsSuccess ? await result.Commands[0].Command.CheckPreconditionsAsync(Context) : null;

			if (!result.IsSuccess)
			{ await ReplyAsync($"The command `{command}` doesn't exist."); return; }
			else if (!preCondCheck.IsSuccess)
			{ await ReplyAsync($"The command `{command}` couldn't be executed.\nReason: " + preCondCheck.ErrorReason); return; }

			string prefix = config["prefix"], aliases = "";
			var embedBuilder = new EmbedBuilder();

			var cmd = result.Commands.First().Command;
			if (cmd.Module.Group == null)
			{
				if (cmd.Aliases.Count > 1) aliases = $"\nAliases: {string.Join(", ", cmd.Aliases)}";
			}
			else aliases = $"\nAliases: {string.Join(", ", cmd.Module.Aliases)}";

			if (result.Commands.First().Command.Module.Group == null)
				embedBuilder.Title = $"{string.Join("\n", result.Commands.Where(c => c.CheckPreconditionsAsync(Context).Result.IsSuccess).Select(c => GetCommandAndParameterString(c.Command)))}\n{aliases}";
			else
				embedBuilder.Title = $"{string.Join("\n", result.Commands.First().Command.Module.Commands.Where(c => c.CheckPreconditionsAsync(Context).Result.IsSuccess).Select(c => GetCommandAndParameterString(c)))}\n{aliases}";
			embedBuilder.Description = cmd.Summary ?? cmd.Module.Summary;

			//if (cmd.Parameters.Count > 0)
			if (result.Commands.Any(c => c.Command.Parameters.Count > 0))
				embedBuilder.Footer = new EmbedFooterBuilder() { Text = "<>: Required⠀⠀[]: Optional\nText within \" \" will be counted as one argument." };

			await ReplyAsync("", false, embedBuilder.Build());
		}

		[Command]
		public async Task Help()
		{
			string prefix = config["prefix"];
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("List of commands")
				.WithDescription($"Prefix: {Format.Code(prefix)}\n\n")
				.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
				.WithFooter("Mentioning the bot works as well as using the prefix.\nUse help <command> to get more info about a command.");

			StringBuilder description = new StringBuilder();
			var commandGroups = service.Commands.GroupBy(x => x.Module.Name);

			foreach (var group in commandGroups)
			{
				string groupCommands = string.Join(", ", group
						.Select(x => x)
						.Where(cmd => cmd.CheckPreconditionsAsync(Context).Result.IsSuccess)
						.Select(x => Format.Code(x.Aliases[0]))
						.Distinct());

				if (!string.IsNullOrEmpty(groupCommands))
					embed.AddField(group.Key, groupCommands);
			}

			await ReplyAsync("", false, embed.Build());
		}

		// Returns the command and its params in the format: commandName <requiredParam> [optionalParam]
		public string GetCommandAndParameterString(CommandInfo cmd)
			=> $"{cmd.Aliases[0]} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**[{p.Name}]**" : $"**[{p.Name} = {p.DefaultValue}]**" : $"**<{p.Name}>**"))}";
	}
}