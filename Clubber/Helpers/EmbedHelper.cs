using Clubber.Models.Responses;
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

		public static Embed Exception(LogMessage msg, IUserMessage? userMessage)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle(msg.Exception?.GetType().Name ?? "Exception thrown")
				.AddField("Severity", msg.Severity, true)
				.AddField("Source", msg.Source, true)
				.WithCurrentTimestamp();

			if (userMessage is not null)
				exceptionEmbed.AddField("User message", Format.Code(userMessage.Content));

			Exception? ex = msg.Exception;

			if (ex is null)
				exceptionEmbed.AddField("Message", msg.Message);

			FillExceptionEmbedBuilder(ex, exceptionEmbed);

			return exceptionEmbed.Build();
		}

		public static Embed Exception(Exception? exception)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle("Cron project - " + (exception?.GetType().Name ?? "Exception thrown"))
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
				exceptionEmbed.AddField(exception.GetType().Name, string.IsNullOrEmpty(exception.Message) ? "No message." : exception.Message);
				exception = exception.InnerException;
			}
		}

		/// <summary>
		/// Returns default stats Embed. For the full stats Embed use <see cref="FullStats(LeaderboardUser, SocketGuildUser?)" />.
		/// </summary>
		public static Embed Stats(EntryResponse lbPlayer, SocketGuildUser? guildUser)
		{
			return new EmbedBuilder()
				.WithTitle($"Stats for {guildUser?.Username ?? lbPlayer.Username}")
				.WithThumbnailUrl(guildUser?.GetAvatarUrl() ?? guildUser?.GetDefaultAvatarUrl() ?? string.Empty)
				.WithDescription($@"✏️ Leaderboard name: {lbPlayer.Username}
🛂 Leaderboard ID: {lbPlayer.Id}
⏱ Score: {lbPlayer.Time / 10000d:0.0000}s
🥇 Rank: {lbPlayer.Rank}
💀 Kills: {lbPlayer.Kills}
♦️ Gems: {lbPlayer.Gems}
🎯 Accuracy: {(double)lbPlayer.DaggersHit / lbPlayer.DaggersFired * 100:0.00}%

• For full stats, use `statsf`.")
				.Build();
		}

		/// <summary>
		/// Returns full stats Embed. For the default stats Embed use <see cref="Stats(LeaderboardUser, SocketGuildUser?)" />.
		/// </summary>
		public static Embed FullStats(EntryResponse lbPlayer, SocketGuildUser? guildUser)
		{
			TimeSpan ts = TimeSpan.FromSeconds((double)lbPlayer.TimeTotal / 10000);
			return new EmbedBuilder()
				.WithTitle($"Stats for {guildUser?.Username ?? lbPlayer.Username}")
				.WithThumbnailUrl(guildUser?.GetAvatarUrl() ?? guildUser?.GetDefaultAvatarUrl() ?? string.Empty)
				.WithDescription($@"✏️ Leaderboard name: {lbPlayer.Username}
🛂 Leaderboard ID: {lbPlayer.Id}
⏱ Score: {lbPlayer.Time / 10000d:0.0000}s
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

		public static Embed WelcomeMessage()
		{
			const string description = @"This is a bot related to the game Devil Daggers. We have roles corresponding to in-game scores ranging from <@&461203024128376832> to <@&903024433315323915>.

If you'd like to have a role, and be able to do stuff like in the image below, feel free to register by posting your in-game name or ID - which you can get from [devildaggers.info](https://devildaggers.info/Leaderboard) (*hover over your rank and it should appear*).

If you don't play the game or simply don't want to be registered, post ""`no score`"". A moderator will then register you.";

			EmbedBuilder embedBuilder = new EmbedBuilder()
				.WithAuthor("Clubber", "https://cdn.discordapp.com/avatars/743431502842298368/04b822b2748acfe2ebaa843522eaba09.webp?size=128")
				.WithTitle("Welcome!")
				.WithDescription(description)
				.WithImageUrl("https://cdn.discordapp.com/attachments/587335375593144321/910859022427652096/PB.png");

			return embedBuilder.Build();
		}
	}
}
