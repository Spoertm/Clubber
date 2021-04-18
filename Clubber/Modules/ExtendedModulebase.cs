using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	public abstract class ExtendedModulebase<T> : ModuleBase<T>
		where T : SocketCommandContext
	{
		public async Task<bool> IsError(bool condition, string output)
		{
			if (!condition)
				return false;

			await InlineReplyAsync(output);
			return true;
		}

		public async Task<IMessage> InlineReplyAsync(string message, bool ping = false)
			=> await ReplyAsync(message, isTTS: false, embed: null, options: null, allowedMentions: ping ? null : AllowedMentions.None, messageReference: new MessageReference(Context.Message.Id));

		public async Task<(bool Success, SocketGuildUser? User)> FoundUserFromDiscordId(ulong discordId)
		{
			SocketGuildUser? user = Context.Guild.GetUser(discordId);

			if (!await IsError(user is null, "User not found."))
				return (true, user);

			return (false, null);
		}

		public async Task<(bool Success, SocketGuildUser? User)> FoundOneUserFromName(string name)
		{
			string trimmedName = name.TrimStart('<', '@', '!').TrimEnd('>');

			if (ulong.TryParse(trimmedName, out ulong userID) && Context.Guild.GetUser(userID) is SocketGuildUser guildUser)
				return (true, guildUser);

			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u =>
				!u.IsBot &&
				(u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				u.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true));

			if (await IsError(!userMatches.Any(), "User not found."))
				return (false, null);

			if (userMatches.Count() > 1)
			{
				await ReplyAsync(message: null, isTTS: false, embed: EmbedHelper.MultipleMatches(userMatches, name), options: null, allowedMentions: AllowedMentions.None, messageReference: new MessageReference(Context.Message.Id));
				return (false, null);
			}

			return (true, userMatches.FirstOrDefault());
		}
	}
}
