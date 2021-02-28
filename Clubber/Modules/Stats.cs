using Clubber.Database;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
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
		public async Task StatsFromCurrentUser() => await CheckUserAndShowStats(Context.Guild.GetUser(Context.User.Id));

		[Command]
		[Remarks("stats clubber\nstats <@743431502842298368>")]
		[Priority(2)]
		public async Task StatsFromName([Name("name | tag")][Remainder] string name)
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
			EmbedBuilder statsEmbed = new();

			if (Context.Message.Content.StartsWith("+statsf") || Context.Message.Content.StartsWith("+statsfull"))
			{
				TimeSpan t = TimeSpan.FromSeconds((double)lbPlayer.TimeTotal / 10000);
				statsEmbed.Description =
$@"✏️ Leaderboard name: {lbPlayer.Username}
🛂 Leaderboard ID: {lbPlayer.Id}
⏱ Score: {lbPlayer.Time / 10000f:0.0000}s
🥇 Rank: {lbPlayer.Rank}
💀 Kills: {lbPlayer.Kills}
💀 Lifetime kills: {lbPlayer.KillsTotal:N0}
♦️ Gems: {lbPlayer.Gems}
♦️ Lifetime gems: {lbPlayer.GemsTotal:N0}
⏱ Total time alive: {t.TotalSeconds:N}s ({t.TotalHours:F0}h {t.Minutes:F0}m {t.Seconds}s)
🗡 Daggers hit: {lbPlayer.DaggersHit:N0}
🗡 Daggers fired: {lbPlayer.DaggersFired:n0}
🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%
🗡 Total daggers hit: {lbPlayer.DaggersHitTotal:N0}
🗡 Total daggers fired: {lbPlayer.DaggersFiredTotal:N0}
🎯 Lifetime accuracy: {(double)lbPlayer.DaggersHitTotal / lbPlayer.DaggersFiredTotal * 100:0.00}%
😵 Total deaths: {lbPlayer.DeathsTotal}
😵 Death type: {Constants.DeathtypeDict[lbPlayer.DeathType]}";
			}
			else
			{
				statsEmbed.Description =
$@"✏️ Leaderboard name: {lbPlayer.Username}
🛂 Leaderboard ID: {lbPlayer.Id}
⏱ Score: {lbPlayer.Time / 10000f:0.0000}s
🥇 Rank: {lbPlayer.Rank}
💀 Kills: {lbPlayer.Kills}
♦️ Gems: {lbPlayer.Gems}
🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%";
			}

			statsEmbed.WithTitle($"Stats for {user!.Username}").WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

			await ReplyAsync(null, false, statsEmbed.Build(), null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}
	}
}
