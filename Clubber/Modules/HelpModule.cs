using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Info"), Group("help")]
	[Summary("Get a list of commands, or info regarding a specific command.")]
	public class HelpModule : AbstractModule<SocketCommandContext>
	{
		private readonly CommandService _service;
		private readonly IConfigurationRoot _config;

		public HelpModule(CommandService service, IConfigurationRoot config)
		{
			_service = service;
			_config = config;
		}

		[Command]
		public async Task Help([Remainder] string command)
		{
			SearchResult result = _service.Search(Context, command);
			PreconditionResult preCondCheck = result.IsSuccess ? await result.Commands[0].Command.CheckPreconditionsAsync(Context) : null;

			if (await IsError(!result.IsSuccess, $"The command `{command}` doesn't exist."))
				return;

			if (await IsError(!preCondCheck.IsSuccess, $"The command `{command}` couldn't be executed.\nReason: " + preCondCheck.ErrorReason))
				return;

			string aliases = string.Empty;
			EmbedBuilder embedBuilder = new();

			CommandInfo cmd = result.Commands[0].Command;
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
				embedBuilder.Footer = new EmbedFooterBuilder { Text = "<>: Required⠀⠀[]: Optional\nText within \" \" will be counted as one argument." };

			await ReplyAsync(string.Empty, false, embedBuilder.Build());
		}

		[Command]
		public async Task Help()
		{
			string prefix = _config["prefix"];
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("List of commands")
				.WithDescription($"Prefix: {Format.Code(prefix)}\n\n")
				.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
				.WithFooter("Mentioning the bot works as well as using the prefix.\nUse help <command> to get more info about a command.");

			foreach (IGrouping<string, CommandInfo> group in _service.Commands.GroupBy(x => x.Module.Name))
			{
				string groupCommands = string.Join(", ", group
					.Select(x => x)
					.Where(cmd => cmd.CheckPreconditionsAsync(Context).Result.IsSuccess)
					.Select(x => Format.Code(x.Aliases[0]))
					.Distinct());

				if (!string.IsNullOrEmpty(groupCommands))
					embed.AddField(group.Key, groupCommands);
			}

			await ReplyAsync(string.Empty, false, embed.Build());
		}

		/// <summary>
		/// Returns the command and its params in the format: commandName <requiredParam> [optionalParam]
		/// </summary>
		public static string GetCommandAndParameterString(CommandInfo cmd)
			=> $"{cmd.Aliases[0]} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**[{p.Name}]**" : $"**[{p.Name} = {p.DefaultValue}]**" : $"**<{p.Name}>**"))}";
	}
}
