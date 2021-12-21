using Clubber.Services;
using Discord;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clubber.BackgroundTasks
{
	public abstract class ExactBackgroundService : BackgroundService
	{
		private readonly LoggingService _loggingService;

		protected ExactBackgroundService(LoggingService loggingService) => _loggingService = loggingService;

		protected abstract TimeOnly UtcTriggerTime { get; }

		protected abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				if (DateTime.UtcNow.Hour != UtcTriggerTime.Hour)
				{
					await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
					continue;
				}

				try
				{
					await ExecuteTaskAsync(stoppingToken);
				}
				catch (Exception exception)
				{
					await _loggingService.LogAsync(new(LogSeverity.Error, "AbstractBackgroundService", string.Empty, exception));
				}

				await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
			}
		}
	}
}
