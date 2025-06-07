using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;

namespace Clubber.Discord.Helpers;

public static class EmbedHelper
{
	public static Embed UpdateRoles(UserRoleUpdate userRoleUpdate)
	{
		IGuildUser user = userRoleUpdate.User;
		RoleUpdate response = userRoleUpdate.RoleUpdate;

		EmbedBuilder embed = new EmbedBuilder()
			.WithTitle($"Updated roles for {user.AvailableNameSanitized()}")
			.WithDescription($"User: {user.Mention}")
			.WithThumbnailUrl(user.GetDisplayAvatarUrl() ?? user.GetDefaultAvatarUrl());

		if (response.RolesToRemove.Count > 0)
		{
			embed.AddField(new EmbedFieldBuilder()
				.WithName("Removed:")
				.WithValue(string.Join('\n', response.RolesToRemove.Select(rr => $"<@&{rr}>")))
				.WithIsInline(true));
		}

		if (response.RolesToAdd.Count > 0)
		{
			embed.AddField(new EmbedFieldBuilder()
				.WithName("Added:")
				.WithValue(string.Join('\n', response.RolesToAdd.Select(ar => $"<@&{ar}>")))
				.WithIsInline(true));
		}

		return embed.Build();
	}

	/// <summary>
	/// Returns default stats Embed. For the full stats Embed use <see cref="FullStats" />.
	/// </summary>
	public static Embed Stats(EntryResponse lbPlayer, SocketGuildUser? guildUser, GetPlayerHistory? playerHistory)
	{
		DateTime? playerPbDatetime = playerHistory?.ScoreHistory.LastOrDefault()?.DateTime;
		string? pbDateTimeFormatted = playerPbDatetime is null ? null : $"\n📅 Achieved on: {playerPbDatetime:yyyy-MM-dd}";
		string sanitizedLbName = Format.Sanitize(lbPlayer.Username);

		return new EmbedBuilder()
			.WithTitle($"Stats for {guildUser?.AvailableNameSanitized() ?? sanitizedLbName}")
			.WithThumbnailUrl(guildUser?.GetDisplayAvatarUrl() ?? guildUser?.GetDefaultAvatarUrl() ?? string.Empty)
			.WithDescription(
				$"""
				 ✏️ Leaderboard name: {sanitizedLbName}
				 🛂 Leaderboard ID: {lbPlayer.Id}
				 ⏲️ Score: {lbPlayer.Time / 10_000d:0.0000}s {pbDateTimeFormatted}
				 🥇 Rank: {lbPlayer.Rank}
				 💀 Kills: {lbPlayer.Kills}
				 ♦️ Gems: {lbPlayer.Gems}
				 🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%

				 • For full stats, use `statsf`.

				 {Format.Url($"{sanitizedLbName} on devildaggers.info", $"https://devildaggers.info/leaderboard/player/{lbPlayer.Id}")}
				 """)
			.Build();
	}

	/// <summary>
	/// Returns full stats Embed. For the default stats Embed use <see cref="Stats" />.
	/// </summary>
	public static Embed FullStats(EntryResponse lbPlayer, SocketGuildUser? guildUser, GetPlayerHistory? playerHistory)
	{
		GetPlayerHistoryScoreEntry? playerPb = playerHistory?.ScoreHistory.LastOrDefault();
		string? peakRankFormatted = playerHistory?.BestRank is null ? null : $"(Best: {playerHistory.BestRank})";
		TimeSpan ts = TimeSpan.FromSeconds((double)lbPlayer.TimeTotal / 10_000);
		string sanitizedLbName = Format.Sanitize(lbPlayer.Username);

		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithTitle($"Stats for {guildUser?.AvailableNameSanitized() ?? sanitizedLbName}")
			.WithThumbnailUrl(guildUser?.GetDisplayAvatarUrl() ?? guildUser?.GetDefaultAvatarUrl() ?? string.Empty)
			.WithDescription(
				$"""
				 ✏️ Leaderboard name: {sanitizedLbName}
				 🛂 Leaderboard ID: {lbPlayer.Id}
				 ⏲️ Score: {lbPlayer.Time / 10_000d:0.0000}s
				 🥇 Rank: {lbPlayer.Rank} {peakRankFormatted}
				 💀 Kills: {lbPlayer.Kills}
				 💀 Lifetime kills: {lbPlayer.KillsTotal:N0}
				 ♦️ Gems: {lbPlayer.Gems}
				 ♦️ Lifetime gems: {lbPlayer.GemsTotal:N0}
				 ⏲️ Total time alive: {ts.TotalSeconds:N}s ({ts.TotalHours:F0}h {ts.Minutes:F0}m {ts.Seconds}s)
				 🗡 Daggers hit: {lbPlayer.DaggersHit:N0}
				 🗡 Daggers fired: {lbPlayer.DaggersFired:n0}
				 🗡 Total daggers hit: {lbPlayer.DaggersHitTotal:N0}
				 🗡 Total daggers fired: {lbPlayer.DaggersFiredTotal:N0}
				 🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%
				 🎯 Lifetime accuracy: {(double)lbPlayer.DaggersHitTotal / lbPlayer.DaggersFiredTotal * 100:0.00}%
				 😵 Total deaths: {lbPlayer.DeathsTotal}
				 😵 Death type: {AppConfig.DeathTypes[lbPlayer.DeathType]}
				 {(playerPb is null ? null : "\u200B")}
				 """);

		if (playerPb != null)
		{
			embedBuilder.AddField("📅 PB achieved on", $"{playerPb.DateTime:yyyy-MM-dd}", true);
		}

		if (playerHistory?.ScoreHistory.Count > 1)
		{
			embedBuilder.AddField("🥈 Previous PB", $"{playerHistory.ScoreHistory[^2].Time:0.0000}s", true);
		}

		if (playerHistory?.ActivityHistory.LastOrDefault(h => h.DeathsIncrement != 0 || h.TimeIncrement != 0) is { } lastActivity)
		{
			embedBuilder.AddField("💤 Last active", $"{lastActivity.DateTime:yyyy-MM-dd}", true);
		}

		embedBuilder.AddField("\u200B",
			Format.Url($"{sanitizedLbName} on devildaggers.info", $"https://devildaggers.info/leaderboard/player/{lbPlayer.Id}"));

		return embedBuilder.Build();
	}

	public static Embed GenericHelp(ICommandContext context, CommandService service)
	{
		EmbedBuilder embed = new EmbedBuilder()
			.WithTitle("List of commands")
			.WithDescription("To check for role updates do `+pb`\nTo get stats do `+me`\n\n")
			.WithThumbnailUrl(context.Client.CurrentUser.GetAvatarUrl())
			.WithFooter("Mentioning the bot works as well as using the prefix.\nUse help <command> to get more info about a command.");

		foreach (IGrouping<string, CommandInfo> group in service.Commands.GroupBy(x => x.Module.Name))
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
		CommandInfo currentCommand = result.Commands[0].Command;

		embedBuilder
			.WithTitle(result.Commands[0].Alias)
			.WithDescription(currentCommand.Summary ?? currentCommand.Module.Summary);

		if (currentCommand.Aliases.Count > 1)
			embedBuilder.AddField("Aliases", string.Join('\n', currentCommand.Aliases), true);

		IEnumerable<CommandInfo> checkedCommands;

		if (currentCommand.Module.Group is null)
			checkedCommands = result.Commands.Where(c => c.CheckPreconditionsAsync(context).Result.IsSuccess).Select(c => c.Command);
		else
			checkedCommands = currentCommand.Module.Commands.Where(c => c.CheckPreconditionsAsync(context).Result.IsSuccess);

		IEnumerable<CommandInfo> commandInfos = checkedCommands as CommandInfo[] ?? checkedCommands.ToArray();
		if (commandInfos.Count() > 1 || commandInfos.Any(cc => cc.Parameters.Count > 0))
		{
			embedBuilder.AddField("Overloads", string.Join('\n', commandInfos.Select(GetCommandAndParameterString)), true);
			embedBuilder.AddField("Examples", string.Join('\n', commandInfos.Select(cc => cc.Remarks)));
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
		IEnumerable<string> parameters = cmd.Parameters.Select(p =>
		{
			if (!p.IsOptional)
				return $"[{p.Name}]";

			return p.DefaultValue is null
				? $"({p.Name})"
				: $"({p.Name} = {p.DefaultValue})";
		});

		return $"{cmd.Aliases[0]} {string.Join(" ", parameters)}";
	}

	public static Embed MultipleMatches(IEnumerable<SocketGuildUser> userMatches, string search)
	{
		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithTitle($"Found multiple matches for '{search.ToLower()}'")
			.WithDescription("Specify their entire username, tag them, or specify their Discord ID in the format `+command id <the id>`.");

		IEnumerable<SocketGuildUser> socketGuildUsers = userMatches as SocketGuildUser[] ?? userMatches.ToArray();
		string userFieldValue = string.Join("\n", socketGuildUsers.Select(um => $"- {FormatUser(um)}"));
		string discordIdFieldValue = string.Join("\n", socketGuildUsers.Select(um => $"- {um.Id}"));

		if (userFieldValue.Length <= 1024 && discordIdFieldValue.Length <= 1024)
		{
			embedBuilder
				.AddField("User", userFieldValue, inline: true)
				.AddField("Discord ID", discordIdFieldValue, inline: true);
		}

		return embedBuilder.Build();
	}

	private static string FormatUser(IGuildUser user)
	{
		string formattedName = user.Nickname is null ? user.Username : $"{user.Username} ({user.Nickname})";
		return Format.Sanitize(formattedName);
	}

	public static Embed[] RegisterEmbeds()
	{
		Embed[] embeds = new Embed[2];

		ulong lowestScoreRoleId = AppConfig.ScoreRoles.MinBy(sr => sr.Key).Value;
		ulong highestScoreRoleId = AppConfig.ScoreRoles.MaxBy(sr => sr.Key).Value;

		string registerForRolesText =
			$"""
			 This bot automatically syncs your roles with your Devil Daggers score. This server has roles corresponding to in-game scores, ranging from <@&{lowestScoreRoleId}> to <@&{highestScoreRoleId}>.

			 If you'd like to have a role and be able to do stuff like in the image below, feel free to register by posting your in-game ID (follow the GIF below).

			 If you don't play the game or simply don't want to be registered, post "`no score`".

			 **After posting the message in this channel, a moderator will then soon register you**.
			 """;

		const string twitchText =
			"""
			You can link your Twitch account to your player page on [DDLIVE](https://ddstats.live/) using "`+twitch MyTwitchUserName`", so others can see who you are in-game when you're live on Twitch.
			""";

		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithTitle("Welcome!")
			.WithImageUrl("https://cdn.discordapp.com/attachments/587335375593144321/910859022427652096/PB.png")
			.AddField(":arrow_forward: Registering for roles", registerForRolesText)
			.AddField(":arrow_forward: Twitch", twitchText);

		embeds[0] = embedBuilder.Build();

		embedBuilder = new EmbedBuilder()
			.WithTitle("How to find your ID")
			.WithDescription("Go to https://devildaggers.info/Leaderboard/search and follow the GIF below.")
			.WithImageUrl("https://cdn.discordapp.com/attachments/587335375593144321/1075788891480670311/HowToFindYourID.gif");

		embeds[1] = embedBuilder.Build();

		return embeds;
	}

	public static Embed UpdatedSplits(BestSplit[] oldBestSplits, BestSplit[] updatedBestSplits)
	{
		Dictionary<string, BestSplit> oldSplitsDict = oldBestSplits.ToDictionary(s => s.Name);
		Dictionary<string, BestSplit> newSplitsDict = updatedBestSplits.ToDictionary(s => s.Name);

		StringBuilder sb = new($"`  {"Split",-9}{"Old",-8}New` Run\n");
		foreach ((string name, int _) in Split.V3Splits)
		{
			oldSplitsDict.TryGetValue(name, out BestSplit? oldSplit);
			newSplitsDict.TryGetValue(name, out BestSplit? newSplit);

			bool isUpdated = newSplit is not null;
			string prefix = isUpdated ? "**`\u22c6 " : "`  ";
			string suffix = isUpdated ? "`**" : "`";

			string oldValue = oldSplit?.Value.ToString() ?? "N/A";
			string newValue = newSplit?.Value.ToString() ?? (oldSplit?.Value.ToString() ?? "N/A");

			string runLink = GetRunLink(newSplit ?? oldSplit);

			sb.AppendLine($"{prefix}{name,-7} {oldValue,4}  {newValue,6}{suffix} {runLink}");
		}

		return new EmbedBuilder()
			.WithTitle("Updated best splits")
			.WithDescription(sb.ToString())
			.Build();

		static string GetRunLink(BestSplit? split)
		{
			return split is not null
				? $"[{split.Description}]({split.GameInfo?.Url})"
				: "N/A";
		}
	}

	public static Embed CurrentBestSplits(BestSplit[] currentBestSplits)
	{
		StringBuilder sb = new();
		sb.Append("Theoretical best peak: ")
			.AppendLine(GetTheoreticalBestPeak(currentBestSplits).ToString())
			.Append($"\n`{"Name",-7}{"Time",-7}{"Split",-5}` Run");

		foreach ((string Name, int Time) split in Split.V3Splits)
		{
			BestSplit? currentBestSplit = Array.Find(currentBestSplits, obs => obs.Name == split.Name);

			string value = "N/A";
			string desc = currentBestSplit?.Description ?? "N/A";
			string descUrl = currentBestSplit is null ? desc : $"[{desc}]({currentBestSplit.GameInfo?.Url})";

			if (currentBestSplit is not null)
				value = currentBestSplit.Name == "350" ? (currentBestSplit.Value - 105).ToString() : currentBestSplit.Value.ToString();

			sb.Append($"\n`{split.Name,-7}{split.Time,4}  {value,6}` {descUrl}");
		}

		return new EmbedBuilder()
			.WithTitle("Current best splits")
			.WithDescription(sb.ToString())
			.Build();
	}

	private static int GetTheoreticalBestPeak(BestSplit[] bestSplits)
	{
		int highest = 0;
		int totalHoming = 0;

		foreach (BestSplit thisSplit in bestSplits)
		{
			if (thisSplit.Name == "350")
				thisSplit.Value += 105;

			totalHoming += thisSplit.Value;

			if (totalHoming > highest)
				highest = totalHoming;
		}

		return highest;
	}

	public static Embed UpdateTopPeakRuns(string userName, HomingPeakRun newRun, HomingPeakRun? oldRun = null, string? avatarUrl = null)
	{
		EmbedBuilder embedBuilder = new();
		if (avatarUrl != null)
		{
			embedBuilder.WithThumbnailUrl(avatarUrl);
		}

		string nameApostrophe = userName.EndsWith("s") ? userName + "'" : userName + "'s";
		if (oldRun != null)
		{
			embedBuilder.WithTitle($"Updated {nameApostrophe} homing peak");
			int homingDiff = newRun.HomingPeak - oldRun.HomingPeak;
			embedBuilder.WithDescription(
				$"""
				 ## [{oldRun.HomingPeak}]({oldRun.Source}) → [{newRun.HomingPeak}]({newRun.Source}) (+{homingDiff})
				 """
			);
		}
		else
		{
			embedBuilder.WithTitle($"Added {nameApostrophe} homing peak");
			embedBuilder.WithDescription($"## [{newRun.HomingPeak}]({newRun.Source}) <:peak:884397348481019924>");
		}

		return embedBuilder.Build();
	}

	public static Embed CurrentTopPeakRuns(HomingPeakRun[] currentTopPeakRuns)
	{
		StringBuilder sb = new();
		for (int i = 0; i < currentTopPeakRuns.Length; i++)
		{
			HomingPeakRun currentPeakRun = currentTopPeakRuns[i];
			sb.Append($"\n{i + 1}. [{currentPeakRun.HomingPeak}]({currentPeakRun.Source}) {Format.Sanitize(currentPeakRun.PlayerName)}");
		}

		return new EmbedBuilder()
			.WithTitle("Top homing peaks")
			.WithDescription(sb.ToString())
			.Build();
	}

	public static Embed RegisterUserModEmbed(string userName, int foundId, string extraInfo)
	{
		EmbedBuilder eb = new();
		eb.WithDescription
		($"""
		  ## Register {userName} with ID `{foundId}`?

		  ### Info about ID {foundId} [from ddinfo](https://devildaggers.info/leaderboard/player/{foundId}):
		  {extraInfo}
		  """);

		return eb.Build();
	}

	public static Embed GiveUserRoleModEmbed(string userName, ulong noScoreRoleId)
	{
		EmbedBuilder eb = new();
		eb.WithDescription($"## Give {userName} {MentionUtils.MentionRole(noScoreRoleId)} role?");

		return eb.Build();
	}
}
