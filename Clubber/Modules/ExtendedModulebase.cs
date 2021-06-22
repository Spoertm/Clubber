using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	public abstract class ExtendedModulebase<T> : ModuleBase<T>
		where T : SocketCommandContext
	{
		protected async Task<bool> IsError(bool condition, string output)
		{
			if (!condition)
				return false;

			await InlineReplyAsync(output);
			return true;
		}

		protected async Task InlineReplyAsync(string message, bool ping = false)
			=> await ReplyAsync(message, allowedMentions: ping ? null : AllowedMentions.None, messageReference: new(Context.Message.Id));

		protected async Task<(bool Success, SocketGuildUser? User)> FoundUserFromDiscordId(ulong discordId)
		{
			SocketGuildUser? user = Context.Guild.GetUser(discordId);

			if (!await IsError(user is null, "User not found."))
				return (true, user);

			return (false, null);
		}

		protected async Task<(bool Success, SocketGuildUser? User)> FoundOneUserFromName(string name)
		{
			string trimmedName = name.TrimStart('<', '@', '!').TrimEnd('>');

			if (ulong.TryParse(trimmedName, out ulong userId) && Context.Guild.GetUser(userId) is { } guildUser)
				return (true, guildUser);

			SocketGuildUser[] userMatches = Context.Guild.Users
				.Where(u =>
					!u.IsBot &&
					(u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
					u.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true))
				.ToArray();

			if (await IsError(userMatches.Length == 0, "User not found."))
				return (false, null);

			if (userMatches.Length == 1)
				return (true, userMatches.FirstOrDefault());

			await ReplyAsync(embed: EmbedHelper.MultipleMatches(userMatches, name), allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
			return (false, null);
		}
	}
}
