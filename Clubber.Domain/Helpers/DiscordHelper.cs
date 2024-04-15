using Clubber.Domain.Models.Exceptions;
using Discord;
using Discord.WebSocket;

namespace Clubber.Domain.Helpers;

public class DiscordHelper : IDiscordHelper
{
	private readonly DiscordSocketClient _client;

	public DiscordHelper(DiscordSocketClient client)
	{
		_client = client;
	}

	public SocketTextChannel GetTextChannel(ulong channelId)
		=> _client.GetChannel(channelId) as SocketTextChannel ?? throw new ClubberException($"No channel with ID {channelId} exists.");

	public SocketGuildUser? GetGuildUser(ulong guildId, ulong userId)
		=> _client.GetGuild(guildId)?.GetUser(userId);

	public SocketGuild? GetGuild(ulong guildId)
		=> _client.GetGuild(guildId);

	public async Task ClearChannelAsync(ITextChannel channel, int exclude = 0)
	{
		DateTimeOffset utcNow = DateTimeOffset.UtcNow;
		IEnumerable<IMessage> lastHundredMessages = await channel.GetMessagesAsync().FlattenAsync();
		IEnumerable<IMessage> messagesToDelete = lastHundredMessages
			.Where(x => (utcNow - x.Timestamp).TotalDays <= 14 && x.Flags is not MessageFlags.Ephemeral)
			.Skip(exclude); // Exclude the n oldest messages

		await channel.DeleteMessagesAsync(messagesToDelete);
	}
}
