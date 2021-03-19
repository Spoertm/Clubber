using Clubber.Database;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Clubber.Helpers
{
	public static class EmbedHelper
	{
		public static Embed GetUpdateRolesEmbed(UpdateRolesResponse response)
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle($"Updated roles for {response.User!.Username}")
				.WithDescription($"User: {response.User!.Mention}")
				.WithThumbnailUrl(response.User!.GetAvatarUrl() ?? response.User!.GetDefaultAvatarUrl());

			if (response.RolesRemoved!.Any())
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Removed:")
					.WithValue(string.Join('\n', response.RolesRemoved!.Select(rr => rr.Mention)))
					.WithIsInline(true));
			}

			if (response.RolesAdded!.Any())
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Added:")
					.WithValue(string.Join('\n', response.RolesAdded!.Select(ar => ar.Mention)))
					.WithIsInline(true));
			}

			return embed.Build();
		}

		public static Embed GetExceptionEmbed(LogMessage msg, SocketUserMessage userMessage)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle(msg.Exception?.GetType().Name ?? "Exception thrown")
				.AddField("Severity", msg.Severity, true)
				.AddField("Source", msg.Source, true)
				.AddField("User message", Format.Code(userMessage.Content))
				.WithCurrentTimestamp();

			Exception? ex = msg.Exception;

			string? exString = ex?.ToString();
			if (exString != null)
			{
				Match regexMatch = Regex.Match(exString, "(?<=   )at.+\n?", RegexOptions.Compiled);
				exceptionEmbed.AddField("Location", regexMatch.Value);
			}

			while (ex != null)
			{
				exceptionEmbed.AddField(ex.GetType().Name, ex.Message ?? "No message.");
				ex = ex.InnerException;
			}

			return exceptionEmbed.Build();
		}

		public static Embed GetExceptionEmbed(Exception? exception)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle("Cron project - " + exception?.GetType().Name ?? "Exception thrown")
				.WithCurrentTimestamp();

			string? exString = exception?.ToString();
			if (exString != null)
			{
				Match regexMatch = Regex.Match(exString, "(?<=   )at.+\n?", RegexOptions.Compiled);
				exceptionEmbed.AddField("Location", regexMatch.Value);
			}

			while (exception != null)
			{
				exceptionEmbed.AddField(exception.GetType().Name, exception.Message ?? "No message.");
				exception = exception.InnerException;
			}

			return exceptionEmbed.Build();
		}

		public static Embed GetStatsEmbed(LeaderboardUser lbPlayer, SocketGuildUser? guildUser, ulong discordId)
		{
			return new EmbedBuilder()
						   .WithTitle($"Stats for {guildUser?.Username ?? discordId.ToString()}")
						   .WithThumbnailUrl(guildUser?.GetAvatarUrl() ?? guildUser?.GetDefaultAvatarUrl() ?? string.Empty)
						   .WithDescription(
$@"✏️ Leaderboard name: {lbPlayer.Username}
🛂 Leaderboard ID: {lbPlayer.Id}
⏱ Score: {lbPlayer.Time / 10000f:0.0000}s
🥇 Rank: {lbPlayer.Rank}
💀 Kills: {lbPlayer.Kills}
♦️ Gems: {lbPlayer.Gems}
🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%")
						   .Build();
		}

		public static Embed GetFullStatsEmbed(LeaderboardUser lbPlayer, SocketGuildUser? guildUser, ulong discordId)
		{
			TimeSpan ts = TimeSpan.FromSeconds((double)lbPlayer.TimeTotal / 10000);
			return new EmbedBuilder()
				.WithTitle($"Stats for {guildUser?.Username ?? discordId.ToString()}")
				.WithThumbnailUrl(guildUser?.GetAvatarUrl() ?? guildUser?.GetDefaultAvatarUrl() ?? string.Empty)
				.WithDescription(
$@"✏️ Leaderboard name: {lbPlayer.Username}
🛂 Leaderboard ID: {lbPlayer.Id}
⏱ Score: {lbPlayer.Time / 10000f:0.0000}s
🥇 Rank: {lbPlayer.Rank}
💀 Kills: {lbPlayer.Kills}
💀 Lifetime kills: {lbPlayer.KillsTotal:N0}
♦️ Gems: {lbPlayer.Gems}
♦️ Lifetime gems: {lbPlayer.GemsTotal:N0}
⏱ Total time alive: {ts.TotalSeconds:N}s ({ts.TotalHours:F0}h {ts.Minutes:F0}m {ts.Seconds}s)
🗡 Daggers hit: {lbPlayer.DaggersHit:N0}
🗡 Daggers fired: {lbPlayer.DaggersFired:n0}
🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%
🗡 Total daggers hit: {lbPlayer.DaggersHitTotal:N0}
🗡 Total daggers fired: {lbPlayer.DaggersFiredTotal:N0}
🎯 Lifetime accuracy: {(double)lbPlayer.DaggersHitTotal / lbPlayer.DaggersFiredTotal * 100:0.00}%
😵 Total deaths: {lbPlayer.DeathsTotal}
😵 Death type: {Constants.DeathtypeDict[lbPlayer.DeathType]}")
				.Build();
		}
	}
}
