﻿using Microsoft.Extensions.Hosting;
using Serilog;

namespace Clubber.Domain.BackgroundTasks;

public abstract class ExactBackgroundService : BackgroundService
{
	protected abstract TimeOnly UtcTriggerTime { get; }

	protected abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await ExecuteIfOnTimeAsync(stoppingToken);
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
			catch (OperationCanceledException operationCanceledException)
			{
				Log.Warning(operationCanceledException, "{ClassName} => service cancellation requested", nameof(ExactBackgroundService));
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Caught exception in {ClassName}", nameof(ExactBackgroundService));
			}
		}

		Log.Warning("{ClassName} => service cancelled", nameof(ExactBackgroundService));
	}

	private async Task ExecuteIfOnTimeAsync(CancellationToken stoppingToken)
	{
		bool isTimeToExecute = (DateTime.UtcNow.Hour, DateTime.UtcNow.Minute) == (UtcTriggerTime.Hour, UtcTriggerTime.Minute);
		if (!isTimeToExecute)
		{
			return;
		}

		await ExecuteTaskAsync(stoppingToken);
	}
}
