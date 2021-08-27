using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class DiscordHelper
	{
		private static SocketTextChannel? _clubberExceptionsChannel;

		protected DiscordHelper(DiscordSocketClient client)
		{
			_clubberExceptionsChannel = client.GetChannel(1123123) as SocketTextChannel;
		}

		public static async Task LogExceptionEmbed(Embed embed)
		{
			if (_clubberExceptionsChannel != null)
				await _clubberExceptionsChannel.SendMessageAsync(embed: embed);
		}
	}
}
