using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace Clubber.Modules
{
	[Name("Info"), Group("Help")]
	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private readonly CommandService service;
		private readonly IConfigurationRoot config;

		public HelpModule(CommandService _service, IConfigurationRoot _config)
		{
			service = _service;
			config = _config;
		}

		[Priority(1)]
		[Command]
		public async Task Help(string command)
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
			if (cmd.Aliases.Count > 1) aliases = $"\nAliases: {string.Join(", ", cmd.Aliases)}";

			embedBuilder.Title = $"{string.Join("\n", result.Commands.Select(c => GetCommandAndParameterString(c.Command)))}{(result.Commands.Count == 1 ? aliases : null)}";
			embedBuilder.Description = cmd.Summary;

			//if (cmd.Parameters.Count > 0)
			if (result.Commands.Any(c => c.Command.Parameters.Count > 0))
				embedBuilder.Footer = new EmbedFooterBuilder() { Text = "<>: Required⠀⠀[]: Optional\nText within \" \" will be counted as one argument." };

			await ReplyAsync("", false, embedBuilder.Build());
		}

		[Priority(0)]
		[Command]
		public async Task Help()
		{
			string prefix = config["prefix"];
			EmbedBuilder builder = new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder
				{
					Name = "List of available commands",
					IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
				}
			};

			StringBuilder description = new StringBuilder();
			foreach (ModuleInfo module in service.Modules)
			{
				description.Clear();
				foreach (var cmd in module.Commands)
				{
					PreconditionResult result = await cmd.CheckPreconditionsAsync(Context);
					if (result.IsSuccess && cmd.Remarks == null)
					{
						int numberOfSimilarCommands = module.Commands.Where(c => c.Name == cmd.Name).Count();
						description.Append($"{cmd.Remarks}{(cmd.Remarks == null ? prefix : "")}{string.Join("/", cmd.Aliases)} ");
						description.Append($"{string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"[{p.Name}]" : $"[{p.Name} = {p.DefaultValue}]" : $"<{p.Name}>"))}");
						if (numberOfSimilarCommands > 1) description.AppendLine($" `(+{numberOfSimilarCommands - 1})`");
						else description.AppendLine();
					}
				}

				if (description.Length != 0)
				{
					builder.AddField(x =>
					{
						x.Name = $"{module.Name}";
						x.Value = description;
						x.IsInline = true;
					});
				}
			}

			builder.AddField("\u200B", $"<>: Required⠀⠀[]: Optional\nText within \" \" will be counted as one argument.\n\nMentioning the bot works as well as using the prefix.\nUse `{prefix}help [command]` or call a command to get more info.");
			await ReplyAsync("", false, builder.Build());
		}

		// Returns the command and its params in the format: commandName <requiredParam> [optionalParam]
		public string GetCommandAndParameterString(CommandInfo cmd)
			=> $"{cmd.Name} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**[{p.Name}]**" : $"**[{p.Name} = {p.DefaultValue}]**" : $"**<{p.Name}>**"))}";
	}
}