using Clubber.Discord.Models;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Exceptions;
using Discord;
using Discord.WebSocket;
using Serilog;

namespace Clubber.Discord.Helpers;

public class DiscordHelper : IDiscordHelper
{
	private readonly ClubberDiscordClient _client;

	public DiscordHelper(ClubberDiscordClient client)
	{
		_client = client;
	}

	public SocketTextChannel GetTextChannel(ulong channelId)
		=> _client.GetChannel(channelId) as SocketTextChannel ?? throw new ClubberException($"No channel with ID {channelId} exists.");

	public SocketGuildUser? GetGuildUser(ulong guildId, ulong userId)
		=> _client.GetGuild(guildId)?.GetUser(userId);

	public SocketGuild? GetGuild(ulong guildId)
		=> _client.GetGuild(guildId);

	public async Task ClearChannelAsync(ITextChannel channel)
	{
		DateTimeOffset utcNow = DateTimeOffset.UtcNow;
		IEnumerable<IMessage> lastHundredMessages = await channel.GetMessagesAsync().FlattenAsync();
		IEnumerable<IMessage> messagesToDelete = lastHundredMessages
			.Where(x => (utcNow - x.Timestamp).TotalDays <= 14 && x.Flags is not MessageFlags.Ephemeral);

		await channel.DeleteMessagesAsync(messagesToDelete);
	}

	public async Task<Result> SendEmbedsEfficientlyAsync(
		Embed[] embeds,
		ulong channelId,
		string? message = null)
	{
		try
		{
			SocketTextChannel channel = GetTextChannel(channelId);

			IEnumerable<Embed[]> embedChunks = embeds.Chunk(DiscordConfig.MaxEmbedsPerMessage);
			foreach (Embed[] embedChunk in embedChunks)
			{
				await channel.SendMessageAsync(message, embeds: embedChunk);
				await Task.Delay(1000);
			}
		}
		catch (Exception e)
		{
			string errorMsg = e switch
			{
				ClubberException     => e.Message,
				HttpRequestException => "Network error.",
				_                    => "Internal error.",
			};

			Log.Error(e, "Failed to send embeds to channel {ChannelId}", channelId);
			return Result.Failure(errorMsg);
		}

		return Result.Success();
	}
}
