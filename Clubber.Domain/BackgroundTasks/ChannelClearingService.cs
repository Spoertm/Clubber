using Clubber.Domain.Helpers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Clubber.Domain.BackgroundTasks;

public class ChannelClearingService : RepeatingBackgroundService
{
	private readonly IConfiguration _config;
	private readonly IDiscordHelper _discordHelper;

	public ChannelClearingService(IConfiguration config, IDiscordHelper discordHelper)
	{
		_config = config;
		_discordHelper = discordHelper;
	}

	protected override TimeSpan TickInterval => TimeSpan.FromHours(1);

	private static readonly TimeSpan _inactivityTime = TimeSpan.FromHours(8);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		ulong registerChannelId = _config.GetValue<ulong>("RegisterChannelId");
		SocketTextChannel registerChannel = _discordHelper.GetTextChannel(registerChannelId);

		IOrderedEnumerable<IMessage> messages = (await registerChannel.GetMessagesAsync(10).FlattenAsync()).OrderBy(x => x.Timestamp);

		if (messages.TryGetNonEnumeratedCount(out int msgCount) && msgCount == 1)
		{
			return;
		}

		// Only clear if there has been no activity
		if (messages.FirstOrDefault() is { } msg && DateTimeOffset.Now - msg.Timestamp >= _inactivityTime)
		{
			await _discordHelper.ClearChannelAsync(registerChannel);
			await registerChannel.SendMessageAsync(embeds: EmbedHelper.RegisterEmbeds());
		}
	}
}
