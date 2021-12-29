using Discord.WebSocket;

namespace Clubber.Helpers;

public interface IDiscordHelper
{
	SocketTextChannel GetTextChannel(ulong channelId);

	SocketGuildUser? GetGuildUser(ulong guildId, ulong userId);

	SocketGuild? GetGuild(ulong guildId);
}
