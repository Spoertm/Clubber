using Clubber.Database;
using Clubber.Files;
using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Info")]
	[Group("me")]
	[Alias("stats", "statsf", "statsfull")]
	[Summary("Provides statistics from the leaderboard for users that are in this server and registered.\n`statsf` shows all the information available.")]
	public class Stats : AbstractModule<SocketCommandContext>
	{
		private readonly DatabaseHelper _databaseHelper;
		private readonly WebService _webService;

		public Stats(DatabaseHelper databaseHelper, WebService webService)
			: base(databaseHelper)
		{
			_databaseHelper = databaseHelper;
			_webService = webService;
		}

		[Command]
		[Remarks("me")]
		[Priority(1)]
		public async Task StatsFromCurrentUser()
			=> await CheckUserAndShowStats(Context.User.Id);

		[Command]
		[Remarks("stats clubber\nstats <@743431502842298368>")]
		[Priority(2)]
		public async Task StatsFromName([Name("name | tag")][Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user is not null)
				await CheckUserAndShowStats(user.Id);
		}

		[Command("id")]
		[Remarks("stats id 743431502842298368")]
		[Priority(3)]
		public async Task StatsFromDiscordId([Name("Discord ID")] ulong discordId)
			=> await CheckUserAndShowStats(discordId);

		private async Task CheckUserAndShowStats(ulong discordId)
		{
			SocketGuildUser? user = Context.Guild.GetUser(discordId);
			DdUser? ddUser = _databaseHelper.GetDdUserByDiscordId(discordId);

			if (ddUser is null)
			{
				if (user is null)
					await InlineReplyAsync("User not found.");
				else
					await UserIsClean(user, checkIfCheater: true, checkIfBot: true, checkIfAlreadyRegistered: false, checkIfNotRegistered: true);

				return;
			}

			await ShowStats(ddUser, user);
		}

		private async Task ShowStats(DdUser ddUser, SocketGuildUser? user)
		{
			uint lbPlayerId = (uint)ddUser.LeaderboardId;
			LeaderboardUser lbPlayer = (await _webService.GetLbPlayers(new uint[] { lbPlayerId }))[0];
			Embed statsEmbed;

			if (Context.Message.Content.StartsWith("+statsf") || Context.Message.Content.StartsWith("+statsfull"))
				statsEmbed = EmbedHelper.FullStats(lbPlayer, user);
			else
				statsEmbed = EmbedHelper.Stats(lbPlayer, user);

			await ReplyAsync(null, false, statsEmbed, null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}
	}
}
