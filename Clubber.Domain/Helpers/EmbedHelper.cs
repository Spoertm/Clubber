using Clubber.Domain.Extensions;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;

namespace Clubber.Domain.Helpers;

public static class EmbedHelper
{
	private static int _maxNameWidth = 10;

	private static readonly Dictionary<int, string> _deathtypeDict = new()
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
				.WithValue(string.Join('\n', response.RolesRemoved!.Select(rr => $"<@&{rr}>")))
				.WithIsInline(true));
		}

		if (response.RolesAdded!.Any())
		{
			embed.AddField(new EmbedFieldBuilder()
				.WithName("Added:")
				.WithValue(string.Join('\n', response.RolesAdded!.Select(ar => $"<@&{ar}>")))
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

		return new EmbedBuilder()
			.WithTitle($"Stats for {guildUser?.Username ?? lbPlayer.Username}")
			.WithThumbnailUrl(guildUser?.GetAvatarUrl() ?? guildUser?.GetDefaultAvatarUrl() ?? string.Empty)
			.WithDescription(
				$"""
				✏️ Leaderboard name: {lbPlayer.Username}
				🛂 Leaderboard ID: {lbPlayer.Id}
				⏲️ Score: {lbPlayer.Time / 10000d:0.0000}s {pbDateTimeFormatted}
				🥇 Rank: {lbPlayer.Rank}
				💀 Kills: {lbPlayer.Kills}
				♦️ Gems: {lbPlayer.Gems}
				🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%

				• For full stats, use `statsf`.

				{Format.Url($"{lbPlayer.Username} on devildaggers.info", $"https://devildaggers.info/leaderboard/player/{lbPlayer.Id}")}
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
		TimeSpan ts = TimeSpan.FromSeconds((double)lbPlayer.TimeTotal / 10000);

		EmbedBuilder? embedBuilder = new EmbedBuilder()
			.WithTitle($"Stats for {guildUser?.Username ?? lbPlayer.Username}")
			.WithThumbnailUrl(guildUser?.GetAvatarUrl() ?? guildUser?.GetDefaultAvatarUrl() ?? string.Empty)
			.WithDescription(
				$"""
				✏️ Leaderboard name: {lbPlayer.Username}
				🛂 Leaderboard ID: {lbPlayer.Id}
				⏲️ Score: {lbPlayer.Time / 10000d:0.0000}s
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
				😵 Death type: {_deathtypeDict[lbPlayer.DeathType]}
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

		embedBuilder.AddField("\u200B", Format.Url($"{lbPlayer.Username} on devildaggers.info", $"https://devildaggers.info/leaderboard/player/{lbPlayer.Id}"));

		return embedBuilder.Build();
	}

	public static Embed GenericHelp(ICommandContext context, CommandService service)
	{
		EmbedBuilder embed = new EmbedBuilder()
			.WithTitle("List of commands")
			.WithDescription("To check for role updates do `+pb`\nTo get stats do `+me`\n\n")
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
		return $"{cmd.Aliases[0]} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue is null ? $"({p.Name})" : $"({p.Name} = {p.DefaultValue})" : $"[{p.Name}]"))}";
	}

	public static Embed MultipleMatches(IEnumerable<SocketGuildUser> userMatches, string search)
	{
		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithTitle($"Found multiple matches for '{search.ToLower()}'")
			.WithDescription("Specify their entire username, tag them, or specify their Discord ID in the format `+command id <the id>`.");

		IEnumerable<SocketGuildUser> socketGuildUsers = userMatches as SocketGuildUser[] ?? userMatches.ToArray();
		string userFieldValue = string.Join("\n", socketGuildUsers.Select(um => $"- {FormatUser(um)}"));
		string discordIdFieldValue = string.Join("\n", socketGuildUsers.Select(um => um.Id));

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
		if (user.Nickname is null)
			return user.Username;

		return $"{user.Username} ({user.Nickname})";
	}

	public static Embed[] RegisterEmbeds(IUser botUser)
	{
		Embed[] embeds = new Embed[2];

		const string registerForRolesText = @"This is a bot related to the game Devil Daggers. We have roles corresponding to in-game scores ranging from <@&461203024128376832> to <@&980126799075876874>.

If you'd like to have a role and be able to do stuff like in the image below, feel free to register by posting your in-game ID - which you can get from [devildaggers.info](https://devildaggers.info/Leaderboard) (*hover over your rank and it should appear*).

If you don't play the game or simply don't want to be registered, post ""`no score`"".

**After posting the message in this channel, a moderator will then soon register you**.";

		const string twitchText = @"You can link your Twitch account to your player page on [DDLIVE](https://ddstats.live/) using ""`+twitch MyTwitchUserName`"", so others can see who you are in-game when you're live on Twitch.";

		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithAuthor(botUser)
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
		int oldSplitsDescriptionPadding = oldBestSplits.MaxBy(obs => obs.Description.Length)?.Description.Length ?? 11;
		int newSplitsDescriptionPadding = updatedBestSplits.MaxBy(obs => obs.Description.Length)?.Description.Length ?? 11;
		int descPadding = Math.Max(oldSplitsDescriptionPadding, newSplitsDescriptionPadding);

		StringBuilder sb = new($"```diff\n{"Name",-8}{"Time",-6}{"Old value",-11}{"New value",-11}{"Description".PadLeft(descPadding)}");
		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithTitle("Updated best splits");

		foreach ((string Name, int Time) split in Split.V3Splits)
		{
			BestSplit? oldBestSplit = Array.Find(oldBestSplits, obs => obs.Name == split.Name);
			BestSplit? newBestSplit = Array.Find(updatedBestSplits, ubs => ubs.Name == split.Name);
			string desc = newBestSplit?.Description ?? oldBestSplit?.Description ?? "N/A";
			sb.Append((oldBestSplit, newBestSplit) switch
			{
				({ }, { })   => $"\n+ {split.Name,-6}{split.Time,4}  {oldBestSplit.Value,9}  {newBestSplit.Value,9}  {desc.PadLeft(descPadding)}",
				({ }, null)  => $"\n= {split.Name,-6}{split.Time,4}  {oldBestSplit.Value,9}  {oldBestSplit.Value,9}  {desc.PadLeft(descPadding)}",
				(null, { })  => $"\n+ {split.Name,-6}{split.Time,4}  {"N/A",9}  {newBestSplit.Value,9}  {desc.PadLeft(descPadding)}",
				(null, null) => $"\n= {split.Name,-6}{split.Time,4}  {"N/A",9}  {"N/A",9}  {desc.PadLeft(descPadding)}",
			});
		}

		embedBuilder.Description = sb.Append("```").ToString();
		return embedBuilder.Build();
	}

	public static Embed CurrentBestSplits(BestSplit[] currentBestSplits)
	{
		int descPadding = currentBestSplits.MaxBy(obs => obs.Description.Length)?.Description.Length ?? 11;
		StringBuilder sb = new();
		sb.Append("Theoretical best peak: ")
			.AppendLine(GetTheoreticalBestPeak(currentBestSplits).ToString())
			.Append($"```{"Name",-6}{"Time",-6}{"Split",-7}{"Description".PadLeft(descPadding)}");

		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithTitle("Current best splits");

		foreach ((string Name, int Time) split in Split.V3Splits)
		{
			BestSplit? currentBestSplit = Array.Find(currentBestSplits, obs => obs.Name == split.Name);

			string value = "N/A";
			string desc = currentBestSplit?.Description ?? "N/A";

			if (currentBestSplit is not null)
				value = currentBestSplit.Name == "350" ? (currentBestSplit.Value - 105).ToString() : currentBestSplit.Value.ToString();

			sb.Append($"\n{split.Name,-6}{split.Time,4}  {value,5}  {desc.PadLeft(descPadding)}");
		}

		embedBuilder.Description = sb.Append("```").ToString();
		return embedBuilder.Build();
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

	public static Embed UpdateTopPeakRuns(IGuildUser user, HomingPeakRun newRun, HomingPeakRun? oldRun = null)
	{
		string name = user.Nickname ?? user.GlobalName ?? user.Username;
		string formattedName = name.EndsWith("s") ? name + "'" : name + "'s";
		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

		if (oldRun != null)
		{
			embedBuilder.WithTitle($"Updated {formattedName} homing peak");
			int homingDiff = newRun.HomingPeak - oldRun.HomingPeak;
			embedBuilder.WithDescription(
				$"""
				## [{oldRun.HomingPeak}]({oldRun.Source}) → [{newRun.HomingPeak}]({newRun.Source}) (+{homingDiff})
				"""
			);
		}
		else
		{
			embedBuilder.WithTitle($"Added {formattedName} homing peak");
			embedBuilder.WithDescription($"## [{newRun.HomingPeak}]({newRun.Source}) <:peak:884397348481019924>");
		}

		return embedBuilder.Build();
	}

	public static Embed CurrentTopPeakRuns(HomingPeakRun[] currentTopPeakRuns)
	{
		int descPadding = currentTopPeakRuns.MaxBy(obs => obs.Source.Length)?.Source.Length ?? 11;
		int maxNumberDigits = currentTopPeakRuns.Length.ToString().Length;
		int nameWidth = Math.Max(4, Math.Min(_maxNameWidth, currentTopPeakRuns.MaxBy(otp => otp.PlayerName.Length)!.PlayerName.Length));

		StringBuilder sb = new();
		sb.Append($"```{"#".PadLeft(maxNumberDigits)}  {"Player".PadRight(nameWidth)}  Peak   {"Source".PadLeft(descPadding)}");

		for (int i = 0; i < currentTopPeakRuns.Length; i++)
		{
			HomingPeakRun currentPeakRun = currentTopPeakRuns[i];
			sb.Append($"\n{(i + 1).ToString().PadLeft(maxNumberDigits)}  {currentPeakRun.PlayerName.Truncate(_maxNameWidth).PadRight(nameWidth)}  {currentPeakRun.HomingPeak,4}   {currentPeakRun.Source.PadLeft(descPadding)}");
		}

		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithTitle("Current top homing peaks")
			.WithDescription(sb.Append("```").ToString());

		return embedBuilder.Build();
	}
}
