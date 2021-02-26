using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Modules
{
	public abstract class AbstractModule<T> : ModuleBase<T>
		where T : SocketCommandContext
	{
		public async Task<bool> IsError(bool condition, string output)
		{
			if (condition)
			{
				await InlineReplayAsync(output);
				return true;
			}

			return false;
		}

		public async Task<IMessage> InlineReplayAsync(string message, bool ping = false)
			=> await ReplyAsync(message, false, null, null, ping ? null : AllowedMentions.None, new MessageReference(Context.Message.Id));

		public async Task<(bool Success, SocketGuildUser? User)> FoundUserFromDiscordId(ulong discordId)
		{
			SocketGuildUser? user = Context.Guild.GetUser(discordId);

			if (!await IsError(user == null, "User not found."))
				return (true, user);
			else
				return (false, null);
		}

		public async Task<(bool Success, SocketGuildUser? User)> FoundOneUserFromName(string name)
		{
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u =>
				u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				u.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true);

			if (!await IsError(!userMatches.Any(), "User not found.") &&
				!await IsError(userMatches.Count() > 1, GetMatchesString(userMatches, name)))
				return (true, userMatches.FirstOrDefault());
			else
				return (false, null);
		}

		private string GetMatchesString(IEnumerable<SocketGuildUser> userMatches, string search)
		{
			string baseMessage = $"Found multiple matches for `{search.ToLower()}`. Specify their entire username, or their Discord ID in the format `+command id <the id>`.";
			string matchesMessage = "\nMatches:\n" + string.Join("\n", userMatches.Select(m => $"- **{m.Username}** ({m.Id})"));

			if (matchesMessage.Length + baseMessage.Length < 2048)
				return baseMessage + matchesMessage;
			else
				return baseMessage;
		}

		public async Task<bool> UserIsClean(SocketGuildUser user, bool checkIfCheater, bool checkIfBot, bool checkIfAlreadyRegistered, bool checkIfNotRegistered)
		{
			if (checkIfBot && user.IsBot)
			{
				await InlineReplayAsync($"{user.Mention} is a bot. It can't be registered as a DD player.");
				return false;
			}

			if (checkIfCheater && user.Roles.Any(r => r.Id == Constants.CheaterRoleId))
			{
				_ = user.Id == Context.User.Id
					? await InlineReplayAsync($"{user.Username}, you can't register because you've cheated.")
					: await InlineReplayAsync($"{user.Username} can't be registered because they've cheated.");

				return false;
			}

			if (checkIfAlreadyRegistered && DatabaseHelper.UserIsRegistered(user.Id))
			{
				await InlineReplayAsync($"User `{user.Username}` is already registered.");
				return false;
			}

			if (checkIfNotRegistered && !DatabaseHelper.UserIsRegistered(user.Id))
			{
				_ = user.Id == Context.User.Id
					? await InlineReplayAsync($"You're not registered, {user.Username}. Please ask an admin/moderator/role assigner to register you.")
					: await InlineReplayAsync($"`{user.Username}` is not registered. Please ask an admin/moderator/role assigner to register them.");

				return false;
			}

			return true;
		}
	}
}
