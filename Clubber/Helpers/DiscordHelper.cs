using Clubber.Models;
using Discord;
using Discord.WebSocket;

namespace Clubber.Helpers
{
	public class DiscordHelper : IDiscordHelper
	{
		private static SocketTextChannel? _clubberExceptionsChannel;
		private readonly DiscordSocketClient _client;

		public DiscordHelper(DiscordSocketClient client)
		{
			ulong clubberExceptionsChannelId = ulong.Parse(Environment.GetEnvironmentVariable("ClubberExceptionsChannelId")!);
			_clubberExceptionsChannel = client.GetChannel(clubberExceptionsChannelId) as SocketTextChannel;
			_client = client;
		}

		public static async Task LogExceptionEmbed(Embed embed)
		{
			if (_clubberExceptionsChannel != null)
				await _clubberExceptionsChannel.SendMessageAsync(embed: embed);
		}

		public SocketTextChannel GetTextChannel(ulong channelId)
			=> _client.GetChannel(channelId) as SocketTextChannel ?? throw new CustomException($"No channel with ID {channelId} exists.");

		public SocketGuildUser? GetGuildUser(ulong guildId, ulong userId)
			=> _client.GetGuild(guildId)?.GetUser(userId);

		public SocketGuild? GetGuild(ulong guildId)
			=> _client.GetGuild(guildId);
	}
}
