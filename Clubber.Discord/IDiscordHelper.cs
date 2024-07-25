using Discord;
using Discord.WebSocket;

namespace Clubber.Discord;

public interface IDiscordHelper
{
	SocketTextChannel GetTextChannel(ulong channelId);

	SocketGuildUser? GetGuildUser(ulong guildId, ulong userId);

	SocketGuild? GetGuild(ulong guildId);

	Task ClearChannelAsync(ITextChannel channel);
}
