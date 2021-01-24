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
		[Command]
		[Priority(3)]
		public async Task StatsFromMention(SocketGuildUser iUser)
		{
			(bool success, SocketGuildUser? user) = await FoundOneGuildUser(iUser.Username);
			if (!success)
				return;
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);

			if (!await UserIsClean(user!, true, true, false, true))
				return;

			dynamic lbPlayer = await DatabaseHelper.GetLbPlayer((uint)DatabaseHelper.DdUsers.Find(du => du.DiscordId == user!.Id)!.LeaderboardId);

			EmbedBuilder embed = new()
			{
				Title = $"Stats for {user!.Username}",
				Description =
$@"✏️ Leaderboard name: {lbPlayer.username}
🛂 Leaderboard ID: {lbPlayer.id}
⏱ Score: {(lbPlayer.time / 10000f).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)}s
🥇 Rank: {lbPlayer.rank}",
				ThumbnailUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
			};

			await ReplyAsync(null, false, embed.Build());
		}

		[Command]
		[Priority(2)]
		public async Task StatsFromName([Remainder] string name)
		{
			if (success && user != null)
				await StatsFromMention(user);
		}

		[Command]
		[Priority(1)]
		public async Task StatsFromCurrentUser() => await StatsFromMention(Context.Guild.GetUser(Context.User.Id));
	}
}
