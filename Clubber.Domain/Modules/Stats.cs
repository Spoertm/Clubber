using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Domain.Modules;

[Name("Info")]
[Group("me")]
[Alias("stats", "statsf", "statsfull", "mef")]
[Summary("Provides statistics from the leaderboard for users that are in this server and registered.\n`statsf` shows all the information available.")]
[RequireContext(ContextType.Guild)]
public class Stats : ExtendedModulebase<SocketCommandContext>
{
	private readonly IDatabaseHelper _databaseHelper;
	private readonly UserService _userService;
	private readonly IWebService _webService;

	public Stats(IDatabaseHelper databaseHelper, UserService userService, IWebService webService)
	{
		_databaseHelper = databaseHelper;
		_userService = userService;
		_webService = webService;
	}

	[Command]
	[Remarks("me")]
	[Priority(1)]
	public async Task StatsFromCurrentUser()
		=> await CheckUserAndShowStats((Context.User as SocketGuildUser)!);

	[Command]
	[Remarks("stats clubber\nstats <@743431502842298368>")]
	[Priority(2)]
	public async Task StatsFromName([Name("name | tag")][Remainder] string name)
	{
		Result<SocketGuildUser> result = await FoundOneUserFromName(name);
		if (result.IsSuccess)
			await CheckUserAndShowStats(result.Value);
	}

	[Command("id")]
	[Remarks("stats id 743431502842298368")]
	[Priority(3)]
	public async Task StatsFromDiscordId([Name("Discord ID")] ulong discordId)
	{
		SocketGuildUser? user = Context.Guild.GetUser(discordId);
		DdUser? ddUser = _databaseHelper.GetDdUserBy(discordId);

		if (ddUser is null)
		{
			if (user is null)
				await InlineReplyAsync("User not found.");
			else
				await InlineReplyAsync(_userService.IsValid(user, user.Id == Context.User.Id).ErrorMsg);

			return;
		}

		await ShowStats(ddUser, user);
	}

	private async Task CheckUserAndShowStats(SocketGuildUser user)
	{
		DdUser? ddUser = _databaseHelper.GetDdUserBy(user.Id);

		if (ddUser is null)
		{
			await InlineReplyAsync(_userService.IsValid(user, user.Id == Context.User.Id).ErrorMsg);
			return;
		}

		await ShowStats(ddUser, user);
	}

	private async Task ShowStats(DdUser ddUser, SocketGuildUser? user)
	{
		uint lbPlayerId = (uint)ddUser.LeaderboardId;
		List<EntryResponse> lbPlayers = await _webService.GetLbPlayers(new[] { lbPlayerId });

		Embed statsEmbed;
		if (Context.Message.Content.StartsWith("+statsf", StringComparison.InvariantCultureIgnoreCase) ||
			Context.Message.Content.StartsWith("+statsfull", StringComparison.InvariantCultureIgnoreCase) ||
			Context.Message.Content.StartsWith("+mef", StringComparison.InvariantCultureIgnoreCase))
		{
			statsEmbed = EmbedHelper.FullStats(lbPlayers[0], user);
		}
		else
		{
			statsEmbed = EmbedHelper.Stats(lbPlayers[0], user);
		}

		await ReplyAsync(embed: statsEmbed, allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
	}
}
