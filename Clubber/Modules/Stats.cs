using Clubber.Database;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Info")]
	[Alias("me")]
	[Group("stats")]
	[Summary("Provides statistics from the leaderboard for users that are in this server and registered.")]
	public class Stats : AbstractModule<SocketCommandContext>
	{
		[Command]
		[Remarks("me")]
		[Priority(1)]
		public async Task StatsFromCurrentUser() => await CheckUserAndShowStats(Context.Guild.GetUser(Context.User.Id));

		[Command]
		[Remarks("stats clubber\nstats <@743431502842298368>")]
		[Priority(2)]
		public async Task StatsFromName([Name("name | tag")] [Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user != null)
				await CheckUserAndShowStats(user);
		}

		[Command("id")]
		[Remarks("stats id 743431502842298368")]
		[Priority(3)]
		public async Task StatsFromDiscordId([Name("Discord ID")] ulong discordId)
		{
			(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
			if (success && user != null)
				await CheckUserAndShowStats(user);
		}

		private async Task CheckUserAndShowStats(SocketGuildUser user)
		{
			if (!await UserIsClean(user, true, true, false, true))
				return;

			LeaderboardUser lbPlayer = await DatabaseHelper.GetLbPlayer((uint)DatabaseHelper.DdUsers.Find(du => du.DiscordId == user!.Id)!.LeaderboardId);

			Embed statsEmbed = new EmbedBuilder()
			{
				Title = $"Stats for {user!.Username}",
				Description =
$@"✏️ Leaderboard name: {lbPlayer.Username}
🛂 Leaderboard ID: {lbPlayer.Id}
⏱ Score: {lbPlayer.Time / 10000f:0.0000}s
🥇 Rank: {lbPlayer.Rank}
💀 Kills: {lbPlayer.Kills}
♦️ Gems: {lbPlayer.Gems}
🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%",
				ThumbnailUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
			}
			.Build();

			await ReplyAsync(null, false, statsEmbed, null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}
	}
}
