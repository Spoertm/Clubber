﻿using Clubber.Database;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Clubber.Helpers
{
	public static class EmbedHelper
	{
		private static readonly Dictionary<short, string> _deathtypeDict = new()
		{
			[0] = "FALLEN",
			[1] = "SWARMED",
			[2] = "IMPALED",
			[3] = "GORED",
			[4] = "INFESTED",
			[5] = "OPENED",
			[6] = "PURGED",
			[7] = "DESECRATED",
			[8] = "SACRIFICED",
			[9] = "EVISCERATED",
			[10] = "ANNIHILATED",
			[11] = "INTOXICATED",
			[12] = "ENVENOMATED",
			[13] = "INCARNATED",
			[14] = "DISCARNATED",
			[15] = "ENTANGLED",
			[16] = "HAUNTED",
		};
		public static Embed UpdateRoles(UpdateRolesResponse response)
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

		public static Embed Exception(LogMessage msg, IUserMessage? userMessage)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle(msg.Exception?.GetType().Name ?? "Exception thrown")
				.AddField("Severity", msg.Severity, true)
				.AddField("Source", msg.Source, true)
				.AddField("User message", Format.Code(userMessage?.Content ?? "null"))
				.WithCurrentTimestamp();

			Exception? ex = msg.Exception;

			if (ex is null)
				exceptionEmbed.AddField("Message", msg.Message);

			string? exString = ex?.ToString();
			if (exString is not null)
			{
				Match regexMatch = Regex.Match(exString, "(?<=   )at.+\n?", RegexOptions.Compiled);
				exceptionEmbed.AddField("Location", regexMatch.Value);
			}

			while (ex is not null)
			{
				exceptionEmbed.AddField(ex.GetType().Name, ex.Message ?? "No message.");
				ex = ex.InnerException;
			}

			return exceptionEmbed.Build();
		}

		public static Embed Exception(Exception? exception)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle("Cron project - " + exception?.GetType().Name ?? "Exception thrown")
				.WithCurrentTimestamp();

			string? exString = exception?.ToString();
			if (exString is not null)
			{
				Match regexMatch = Regex.Match(exString, "(?<=   )at.+\n?", RegexOptions.Compiled);
				exceptionEmbed.AddField("Location", regexMatch.Value);
			}

			while (exception is not null)
			{
				exceptionEmbed.AddField(exception.GetType().Name, exception.Message ?? "No message.");
				exception = exception.InnerException;
			}

			return exceptionEmbed.Build();
		}

		/// <summary>
		/// Returns default stats Embed. For the full stats Embed use <see cref="FullStats(LeaderboardUser, SocketGuildUser?, ulong)"/>.
		/// </summary>
		public static Embed Stats(LeaderboardUser lbPlayer, SocketGuildUser? guildUser)
		{
			return new EmbedBuilder()
						   .WithTitle($"Stats for {guildUser?.Username ?? lbPlayer.Username}")
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

		/// <summary>
		/// Returns full stats Embed. For the default stats Embed use <see cref="Stats(LeaderboardUser, SocketGuildUser?, ulong)"/>.
		/// </summary>
		public static Embed FullStats(LeaderboardUser lbPlayer, SocketGuildUser? guildUser)
		{
			TimeSpan ts = TimeSpan.FromSeconds((double)lbPlayer.TimeTotal / 10000);
			return new EmbedBuilder()
				.WithTitle($"Stats for {guildUser?.Username ?? lbPlayer.Username}")
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
😵 Death type: {_deathtypeDict[lbPlayer.DeathType]}")
				.Build();
		}
	}
}
