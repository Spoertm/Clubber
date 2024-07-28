using Clubber.Discord.Helpers;
using Clubber.Discord.Services;
using Discord;
using Discord.Commands;

namespace Clubber.Discord.Modules;

[RequireOwner]
public class Owner : ExtendedModulebase<SocketCommandContext>
{
	private readonly ScoreRoleService _scoreRoleService;

	public Owner(ScoreRoleService scoreRoleService)
	{
		_scoreRoleService = scoreRoleService;
	}

	[Command("update database")]
	[RequireContext(ContextType.Guild)]
	public async Task UpdateDatabase()
	{
		const string checkingString = "Checking for role updates...";
		IUserMessage msg = await ReplyAsync(checkingString);

		(string message, Embed[] roleUpdateEmbeds) = await _scoreRoleService.UpdateRolesAndDb(Context.Guild.Users);
		await msg.ModifyAsync(m => m.Content = $"{checkingString}\n{message}");

		for (int i = 0; i < roleUpdateEmbeds.Length; i++)
			await ReplyAsync(null, false, roleUpdateEmbeds[i]);
	}

	[Command("welcome")]
	[RequireContext(ContextType.Guild)]
	public async Task PostWelcome() => await Context.Channel.SendMessageAsync(embeds: EmbedHelper.RegisterEmbeds());
}
