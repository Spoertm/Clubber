using Clubber.Configuration;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class DiscordHelper
	{
		private static SocketTextChannel? _clubberExceptionsChannel;
		private readonly DiscordSocketClient _client;

		public DiscordHelper(IConfig config, DiscordSocketClient client)
		{
			_clubberExceptionsChannel = client.GetChannel(config.ClubberExceptionsChannelId) as SocketTextChannel;
			_client = client;
		}

		public static async Task LogExceptionEmbed(Embed embed)
		{
			if (_clubberExceptionsChannel != null)
				await _clubberExceptionsChannel.SendMessageAsync(embed: embed);
		}

		public SocketTextChannel GetTextChannel(ulong channelId)
			=> _client.GetChannel(channelId) as SocketTextChannel ?? throw new($"No channel with ID {channelId} exists.");

		public async Task SendFileToChannel(string filePath, ulong channelId, string? text = null)
			=> await GetTextChannel(channelId).SendFileAsync(filePath, text);

		public async Task<string> GetLatestAttachmentUrlFromChannel(ulong channelId)
		{
			SocketTextChannel channel = GetTextChannel(channelId);
			IAttachment? latestAttachment = (await channel.GetMessagesAsync(1).FlattenAsync())
				.FirstOrDefault()?
				.Attachments
				.FirstOrDefault() ?? throw new($"No files in {channel.Name} channel.");

			return latestAttachment.Url;
		}

		public SocketGuildUser? GetGuildUser(ulong guildId, ulong userId)
			=> _client.GetGuild(guildId)?.GetUser(userId);
	}
}
