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

			if (!await IsError(user == null, "User not found."))
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
				if (user != null)
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

		private string GetMatchesString(IEnumerable<SocketGuildUser> userMatches, string search)
		{
			string baseMessage = $"Found multiple matches for `{search.ToLower()}`.\nSpecify their entire username, tag them, or specify their Discord ID in the format `+command id <the id>`.";

			int padding = userMatches.Max(um => um.Username.Length + (um.Nickname?.Length + 3 ?? 0)) + 2;
			string matchesMessage = "\n\nMatches:\n" + string.Join("\n", userMatches
				.Select(m => Format.Code($"{(m.Username + (m.Nickname is null ? null : $" ({m.Nickname})")).PadRight(padding)}{m.Id}")));

			if (matchesMessage.Length + baseMessage.Length < 2048)
				return baseMessage + matchesMessage;
			else
				return baseMessage;
		}

		public async Task<bool> UserIsClean(SocketGuildUser user, bool checkIfCheater, bool checkIfBot, bool checkIfAlreadyRegistered, bool checkIfNotRegistered)
		{
			if (checkIfBot && user.IsBot)
			{
				await InlineReplyAsync($"{user.Mention} is a bot. It can't be registered as a DD player.");
				return false;
			}

			if (checkIfCheater && user.Roles.Any(r => r.Id == Constants.CheaterRoleId))
			{
				_ = user.Id == Context.User.Id
					? await InlineReplyAsync($"{user.Username}, you can't register because you've cheated.")
					: await InlineReplyAsync($"{user.Username} can't be registered because they've cheated.");

				return false;
			}

			if (checkIfAlreadyRegistered && DatabaseHelper.GetDdUser(user.Id) is not null)
			{
				await InlineReplyAsync($"User `{user.Username}` is already registered.");
				return false;
			}

			if (checkIfNotRegistered && DatabaseHelper.GetDdUser(user.Id) is null)
			{
				if ((Context.User as SocketGuildUser)!.GuildPermissions.ManageRoles)
				{
					_ = await InlineReplyAsync($"`{user.Username}` is not registered.");
					return false;
				}

				_ = user.Id == Context.User.Id
					? await InlineReplyAsync($"You're not registered, {user.Username}. Only a <@&{Constants.RoleAssignerRoleId}> can register you, and one should be here soon.\nPlease refer to the message in <#{Constants.RegisterChannel}> for more info.")
					: await InlineReplyAsync($"`{user.Username}` is not registered. Only a <@&{Constants.RoleAssignerRoleId}> can register them, and one should be here soon.\nPlease refer to the message in <#{Constants.RegisterChannel}> for more info.");

				return false;
			}

			return true;
		}
	}
}
