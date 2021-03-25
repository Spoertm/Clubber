using Clubber.Helpers;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[RequireContext(ContextType.Guild)]
	public class Info : ExtendedModulebase<SocketCommandContext>
	{
		private readonly CommandService _commands;

		public Info(CommandService commands)
		{
			_commands = commands;
		}

		[Command("whyareyou")]
		[Summary("Describes what the bot does.")]
		public async Task WhyAreYou() => await InlineReplyAsync(Constants.WhyAreYou);

		[Command("help")]
		[Summary("Get a list of commands, or info regarding a specific command.")]
		[Priority(0)]
		public async Task Help()
		{
			Embed embed = EmbedHelper.GenericHelp(Context, _commands);
			await ReplyAsync(null, false, embed, null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}

		[Command("help")]
		[Summary("Get a list of commands, or info regarding a specific command.")]
		[Remarks("help pb\nhelp stats")]
		[Priority(1)]
		public async Task Help([Remainder] string command)
		{
			SearchResult searchResult = _commands.Search(Context, command);
			if (await IsError(!searchResult.IsSuccess, $"The command `{command}` doesn't exist."))
				return;

			PreconditionResult preCondCheck = await searchResult.Commands[0].Command.CheckPreconditionsAsync(Context);
			if (await IsError(!preCondCheck.IsSuccess, $"The command `{command}` couldn't be executed.\nReason: " + preCondCheck.ErrorReason))
				return;

			Embed embed = EmbedHelper.CommandHelp(Context, searchResult);

			await ReplyAsync(null, false, embed, null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}
	}
}
