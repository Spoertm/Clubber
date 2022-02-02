using Clubber.Models;
using Discord.WebSocket;

namespace Clubber.Helpers;

public class DiscordHelper : IDiscordHelper
{
	private readonly DiscordSocketClient _client;

	public DiscordHelper(DiscordSocketClient client)
	{
		_client = client;
	}

	public SocketTextChannel GetTextChannel(ulong channelId)
		=> _client.GetChannel(channelId) as SocketTextChannel ?? throw new CustomException($"No channel with ID {channelId} exists.");

	public SocketGuildUser? GetGuildUser(ulong guildId, ulong userId)
		=> _client.GetGuild(guildId)?.GetUser(userId);

	public SocketGuild? GetGuild(ulong guildId)
		=> _client.GetGuild(guildId);
}
