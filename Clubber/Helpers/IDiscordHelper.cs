using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public interface IDiscordHelper
	{
		SocketTextChannel GetTextChannel(ulong channelId);

		Task SendFileToChannel(string filePath, ulong channelId, string? text = null);

		SocketGuildUser? GetGuildUser(ulong guildId, ulong userId);

		SocketGuild? GetGuild(ulong guildId);
	}
}
