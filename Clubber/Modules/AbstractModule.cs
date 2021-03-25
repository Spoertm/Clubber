using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	public abstract class AbstractModule<T> : ModuleBase<T>
		where T : SocketCommandContext
	{
		public async Task<bool> IsError(bool condition, string output)
		{
			if (condition)
			{
				await InlineReplyAsync(output);
				return true;
			}

			return false;
		}

		public async Task<IMessage> InlineReplyAsync(string message, bool ping = false)
			=> await ReplyAsync(message, false, null, null, ping ? null : AllowedMentions.None, new MessageReference(Context.Message.Id));

		public async Task<(bool Success, SocketGuildUser? User)> FoundUserFromDiscordId(ulong discordId)
		{
			SocketGuildUser? user = Context.Guild.GetUser(discordId);

			if (!await IsError(user is null, "User not found."))
				return (true, user);
			else
				return (false, null);
		}

		public async Task<(bool Success, SocketGuildUser? User)> FoundOneUserFromName(string name)
		{
			string trimmedName = name.TrimStart('<', '@', '!').TrimEnd('>');

			if (ulong.TryParse(trimmedName, out ulong userID))
			{
				SocketGuildUser? user = Context.Guild.GetUser(userID);
				if (user is not null)
					return (true, user);
			}

			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u =>
				!u.IsBot &&
				(u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				u.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true));

			if (!await IsError(!userMatches.Any(), "User not found.") &&
				!await IsError(userMatches.Count() > 1, GetMatchesString(userMatches, name)))
				return (true, userMatches.FirstOrDefault());
			else
				return (false, null);
		}

		private static string GetMatchesString(IEnumerable<SocketGuildUser> userMatches, string search)
		{
			string baseMessage = $"Found multiple matches for `{search.ToLower()}`.\nSpecify their entire username, tag them, or specify their Discord ID in the format `+command id <the id>`.";

			int padding = userMatches.Max(um => um.Username.Length + (um.Nickname?.Length + 3 ?? 0)) + 2;
			string matchesMessage = "\n\nMatches:\n" + string.Join("\n", userMatches
				.Select(m => FormatUser(m, padding)));

			if (matchesMessage.Length + baseMessage.Length < 2048)
				return baseMessage + matchesMessage;
			else
				return baseMessage;
		}

		private static string FormatUser(SocketGuildUser user, int padding)
		{
			string formattedNickname = user.Nickname is null ? string.Empty : $" ({user.Nickname})";
			return Format.Code($"{(user.Username + formattedNickname).PadRight(padding)}{user.Id}");
		}
	}
}
