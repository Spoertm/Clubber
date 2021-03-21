using Clubber.Database;
using Clubber.Files;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Info")]
	[Group("me")]
	[Alias("stats", "statsf", "statsfull")]
	[Summary("Provides statistics from the leaderboard for users that are in this server and registered.\n`statsf` shows all the information available.")]
	public class Stats : AbstractModule<SocketCommandContext>
	{
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
			if (success && user != null)
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
			DdUser? ddUser = DatabaseHelper.GetDdUser(discordId);

			if (ddUser is null)
			{
				if (user is null)
					await InlineReplyAsync("User not found.");
				else
					await UserIsClean(user, true, true, false, true);

				return;
			}

			await ShowStats(ddUser, user);
		}

		private async Task ShowStats(DdUser ddUser, SocketGuildUser? user)
		{
			uint lbPlayerId = (uint)ddUser.LeaderboardId;
			LeaderboardUser lbPlayer = (await DatabaseHelper.GetLbPlayers(new uint[] { lbPlayerId })).First();
			Embed statsEmbed;

			if (Context.Message.Content.StartsWith("+statsf") || Context.Message.Content.StartsWith("+statsfull"))
				statsEmbed = EmbedHelper.FullStats(lbPlayer, user);
			else
				statsEmbed = EmbedHelper.Stats(lbPlayer, user);

			await ReplyAsync(null, false, statsEmbed, null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}
	}
}
