using Clubber.Database;
using Discord;
using Discord.Commands;
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
		private static readonly Regex _exceptionRegex = new("(?<=   )at.+\n?", RegexOptions.Compiled);

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

			FillExceptionEmbedBuilder(ex, exceptionEmbed);

			return exceptionEmbed.Build();
		}

		public static Embed Exception(Exception? exception)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle("Cron project - " + exception?.GetType().Name ?? "Exception thrown")
				.WithCurrentTimestamp();

			FillExceptionEmbedBuilder(exception, exceptionEmbed);

			return exceptionEmbed.Build();
		}

		private static void FillExceptionEmbedBuilder(Exception? exception, EmbedBuilder exceptionEmbed)
		{
			string? exString = exception?.ToString();
			if (exString is not null)
			{
				Match regexMatch = _exceptionRegex.Match(exString);
				exceptionEmbed.AddField("Location", regexMatch.Value);
			}

			while (exception is not null)
			{
				exceptionEmbed.AddField(exception.GetType().Name, exception.Message ?? "No message.");
				exception = exception.InnerException;
			}
		}

		/// <summary>
		/// Returns default stats Embed. For the full stats Embed use <see cref="FullStats(LeaderboardUser, SocketGuildUser?)"/>.
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
		/// Returns full stats Embed. For the default stats Embed use <see cref="Stats(LeaderboardUser, SocketGuildUser?)"/>.
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

		public static Embed GenericHelp(ICommandContext context, CommandService service)
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("List of commands")
				.WithDescription($"To check for role updates do `{Constants.Prefix}pb`\nTo get stats do `{Constants.Prefix}me`\n\n")
				.WithThumbnailUrl(context.Client.CurrentUser.GetAvatarUrl())
				.WithFooter("Mentioning the bot works as well as using the prefix.\nUse help <command> to get more info about a command.");

			foreach (IGrouping<string, CommandInfo>? group in service.Commands.GroupBy(x => x.Module.Name))
			{
				string groupCommands = string.Join(", ", group
					.Where(cmd => cmd.CheckPreconditionsAsync(context).Result.IsSuccess)
					.Select(x => Format.Code(x.Aliases[0]))
					.Distinct());

				if (!string.IsNullOrEmpty(groupCommands))
					embed.AddField(group.Key, groupCommands);
			}

			return embed.Build();
		}

		public static Embed CommandHelp(ICommandContext context, SearchResult result)
		{
			EmbedBuilder embedBuilder = new();
			CommandInfo cmd = result.Commands[0].Command;

			embedBuilder
				.WithTitle(result.Commands[0].Alias)
				.WithDescription(cmd.Summary ?? cmd.Module.Summary);

			if (cmd.Aliases.Count > 1)
				embedBuilder.AddField("Aliases", string.Join('\n', result.Commands[0].Command.Aliases), true);

			IEnumerable<CommandInfo> checkedCommands;

			if (result.Commands[0].Command.Module.Group is null)
				checkedCommands = result.Commands.Where(c => c.CheckPreconditionsAsync(context).Result.IsSuccess).Select(c => c.Command);
			else
				checkedCommands = result.Commands[0].Command.Module.Commands.Where(c => c.CheckPreconditionsAsync(context).Result.IsSuccess);

			if (checkedCommands.Count() > 1 || checkedCommands.Any(cc => cc.Parameters.Count > 0))
			{
				embedBuilder.AddField("Overloads", string.Join('\n', checkedCommands.Select(cc => GetCommandAndParameterString(cc))), true);
				embedBuilder.AddField("Examples", string.Join('\n', checkedCommands.Select(cc => cc.Remarks)));
			}

			if (result.Commands.Any(c => c.Command.Parameters.Count > 0))
				embedBuilder.WithFooter("[]: Required⠀⠀(): Optional\nText within \" \" will be counted as one argument.");

			return embedBuilder.Build();
		}

		/// <summary>
		/// Returns the command and its params in the format: commandName [requiredParam] (optionalParam).
		/// </summary>
		private static string GetCommandAndParameterString(CommandInfo cmd)
		{
			return $"{cmd.Aliases[0]} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue is null ? $"({p.Name})" : $"({p.Name} = {p.DefaultValue})" : $"[{p.Name}]"))}";
		}

		public static Embed MultipleMatches(IEnumerable<SocketGuildUser> userMatches, string search)
		{
			EmbedBuilder embedBuilder = new EmbedBuilder()
				.WithTitle($"Found multiple matches for '{search.ToLower()}'")
				.WithDescription("Specify their entire username, tag them, or specify their Discord ID in the format `+command id <the id>`.");

			string userFieldValue = string.Join("\n", userMatches.Select(um => $"- {FormatUser(um)}"));
			string discordIdFieldValue = string.Join("\n", userMatches.Select(um => um.Id));

			if (userFieldValue.Length <= 1024 && discordIdFieldValue.Length <= 1024)
			{
				embedBuilder
					.AddField("User", userFieldValue, inline: true)
					.AddField("Discord ID", discordIdFieldValue, inline: true);
			}

			return embedBuilder.Build();
		}

		private static string FormatUser(SocketGuildUser user)
		{
			if (user.Nickname is null)
				return user.Username;

			return $"{user.Username} ({user.Nickname})";
		}
	}
}
