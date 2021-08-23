using Clubber.Services;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clubber.BackgroundTasks
{
	public class DdNewsPostService : AbstractBackgroundService
	{
		private readonly DiscordSocketClient _client;

		public DdNewsPostService(DiscordSocketClient client, LoggingService loggingService)
			: base(loggingService)
		{
			_client = client;
		}

		protected override TimeSpan Interval => TimeSpan.FromMinutes(2);

		protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
		{
		}
	}
}
