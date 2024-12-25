using Clubber.Discord.Helpers;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Clubber.Discord.Services;

public class ChannelClearingService : RepeatingBackgroundService
{
	private readonly IOptionsMonitor<BotConfig> _botConfig;
	private readonly IDiscordHelper _discordHelper;
	private readonly EmbedHelper _embedHelper;
	private static readonly TimeSpan _inactivityTime = TimeSpan.FromHours(8);

	public ChannelClearingService(IOptionsMonitor<BotConfig> botConfig, IDiscordHelper discordHelper, EmbedHelper embedHelper)
	{
		_botConfig = botConfig;
		_discordHelper = discordHelper;
		_embedHelper = embedHelper;
	}

	protected override TimeSpan TickInterval => TimeSpan.FromHours(1);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		SocketTextChannel registerChannel = _discordHelper.GetTextChannel(_botConfig.CurrentValue.RegisterChannelId);

		IOrderedEnumerable<IMessage> messages = (await registerChannel.GetMessagesAsync(10).FlattenAsync()).OrderBy(x => x.Timestamp);

		if (messages.TryGetNonEnumeratedCount(out int msgCount) && msgCount == 1)
		{
			return;
		}

		// Only clear if there has been no activity
		if (messages.FirstOrDefault() is { } msg && DateTimeOffset.Now - msg.Timestamp >= _inactivityTime)
		{
			await _discordHelper.ClearChannelAsync(registerChannel);
			await registerChannel.SendMessageAsync(embeds: _embedHelper.RegisterEmbeds());
		}
	}
}
