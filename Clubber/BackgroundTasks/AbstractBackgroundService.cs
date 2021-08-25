﻿using Clubber.Services;
using Discord;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clubber.BackgroundTasks
{
	public abstract class AbstractBackgroundService : BackgroundService
	{
		private readonly LoggingService _loggingService;

		protected AbstractBackgroundService(LoggingService loggingService)
		{
			_loggingService = loggingService;
		}

		protected abstract TimeSpan Interval { get; }

		protected abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await ExecuteTaskAsync(stoppingToken);
				}
				catch (Exception exception)
				{
					await _loggingService.LogAsync(new(LogSeverity.Error, "AbstractBackgroundService", string.Empty, exception));
				}

				if (Interval.TotalMilliseconds > 0)
					await Task.Delay(Interval, stoppingToken);
			}

			await _loggingService.LogAsync(new(LogSeverity.Warning, "AbstractBackgroundService", "Service cancelled."));
		}
	}
}