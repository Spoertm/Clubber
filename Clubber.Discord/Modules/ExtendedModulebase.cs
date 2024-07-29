using Clubber.Discord.Helpers;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Discord.Modules;

public abstract class ExtendedModulebase<T> : ModuleBase<T>
	where T : SocketCommandContext
{
	protected async Task<bool> IsError(bool condition, string output)
	{
		if (!condition)
		{
			return false;
		}

		await InlineReplyAsync(output);
		return true;
	}

	protected async Task InlineReplyAsync(string message, bool ping = false)
		=> await ReplyAsync(message, allowedMentions: ping ? null : AllowedMentions.None, messageReference: new(Context.Message.Id));

	protected async Task<Result<SocketGuildUser>> FoundUserFromDiscordId(ulong discordId)
	{
		SocketGuildUser user = Context.Guild.GetUser(discordId);

		if (!await IsError(user is null, "User not found."))
		{
			return Result.Success(user!);
		}

		return Result.Failure<SocketGuildUser>("User not found.")!;
	}

	protected async Task<Result<SocketGuildUser>> FoundOneUserFromName(string name)
	{
		string trimmedName = name.TrimStart('<', '@', '!').TrimEnd('>');

		if (ulong.TryParse(trimmedName, out ulong userId) && Context.Guild.GetUser(userId) is { } guildUser)
		{
			return Result.Success(guildUser);
		}

		SocketGuildUser[] userMatches = Context.Guild.Users
			.Where(u =>
				!u.IsBot &&
				(u.Username.Contains(name, StringComparison.OrdinalIgnoreCase) ||
				u.Nickname?.Contains(name, StringComparison.OrdinalIgnoreCase) == true))
			.ToArray();

		if (await IsError(userMatches.Length == 0, "User not found."))
		{
			return Result.Failure<SocketGuildUser>("User not found.")!;
		}

		if (userMatches.Length == 1)
		{
			return Result.Success(userMatches[0]);
		}

		await ReplyAsync(embed: EmbedHelper.MultipleMatches(userMatches, name), allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
		return Result.Failure<SocketGuildUser>("Found multiple matches.")!;
	}
}
