using Clubber.Database;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Info")]
	[Group("stats")]
	[Summary("Provides statistics from the leaderboard.")]
	public class Stats : AbstractModule<SocketCommandContext>
	{
		[Command("id")]
		[Priority(3)]
		public async Task StatsFromDiscordId(ulong discordId)
		{
			(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
			if (success && user != null)
				await CheckUserAndShowStats(user);
		}

		[Command]
		[Priority(2)]
		public async Task StatsFromName([Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user != null)
				await CheckUserAndShowStats(user);
		}

		[Command]
		[Priority(1)]
		public async Task StatsFromCurrentUser() => await CheckUserAndShowStats(Context.Guild.GetUser(Context.User.Id));

		private async Task CheckUserAndShowStats(SocketGuildUser user)
		{
			if (!await UserIsClean(user, true, true, false, true))
				return;

			LeaderboardUser lbPlayer = await DatabaseHelper.GetLbPlayer((uint)DatabaseHelper.DdUsers.Find(du => du.DiscordId == user!.Id)!.LeaderboardId);

			await ReplyAsync(null, false, new EmbedBuilder()
			{
				Title = $"Stats for {user!.Username}",
				Description =
$@"✏️ Leaderboard name: {lbPlayer.Username}
🛂 Leaderboard ID: {lbPlayer.Id}
⏱ Score: {(lbPlayer.Time / 10000f).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)}s
🥇 Rank: {lbPlayer.Rank}",
				ThumbnailUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
			}.Build());
		}
	}
}
