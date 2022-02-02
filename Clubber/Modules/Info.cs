using Clubber.Helpers;
using Discord;
using Discord.Commands;

namespace Clubber.Modules;

[RequireContext(ContextType.Guild)]
public class Info : ExtendedModulebase<SocketCommandContext>
{
	private readonly CommandService _commands;

	public Info(CommandService commands) => _commands = commands;

	[Command("whyareyou")]
	[Summary("Describes what the bot does.")]
	public async Task WhyAreYou()
	{
		const string whyAreYouText = @"Every day or so, I automatically update people's DD roles.
For example, if someone beats their score of 300s and gets 400s, I update their role from `300+ club` to `400+ club`.
To speed this up, you can manually update your own or someone else's roles by using the `+pb` or `+updateroles` command.";

		await InlineReplyAsync(whyAreYouText);
	}

	[Command("help")]
	[Summary("Get a list of commands, or info regarding a specific command.")]
	[Priority(0)]
	public async Task Help()
	{
		Embed embed = EmbedHelper.GenericHelp(Context, _commands);
		await ReplyAsync(embed: embed, allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
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
		await ReplyAsync(embed: embed, allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
	}
}
