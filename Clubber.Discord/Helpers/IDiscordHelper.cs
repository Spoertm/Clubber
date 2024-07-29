using Clubber.Domain.Models;
using Discord;
using Discord.WebSocket;

namespace Clubber.Discord.Helpers;

public interface IDiscordHelper
{
	SocketTextChannel GetTextChannel(ulong channelId);

	SocketGuildUser? GetGuildUser(ulong guildId, ulong userId);

	SocketGuild? GetGuild(ulong guildId);

	Task ClearChannelAsync(ITextChannel channel);

	Task<Result> SendEmbedsEfficientlyAsync(Embed[] embeds, ulong channelId, string? message = null);
}
