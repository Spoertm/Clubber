using Microsoft.Extensions.Hosting;
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
				TimeSpan delay = CalculateDelayUntilNextTrigger();
				await Task.Delay(delay, stoppingToken).ConfigureAwait(false);

				// Double-check we're still running before executing
				if (stoppingToken.IsCancellationRequested)
				{
					break;
				}

				Log.Information("{ClassName} => executing scheduled task", GetType().Name);
				await ExecuteTaskAsync(stoppingToken).ConfigureAwait(false);

				// Small delay to prevent re-triggering within the same minute
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Normal shutdown, exit cleanly
				break;
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Caught exception in {ClassName}", GetType().Name);

				// Prevent tight loop on persistent errors
				// Use CancellationToken.None to ensure we always wait, even during shutdown
				await Task.Delay(TimeSpan.FromMinutes(1), CancellationToken.None).ConfigureAwait(false);
			}
		}

		Log.Information("{ClassName} => stopped gracefully", GetType().Name);
	}

	private TimeSpan CalculateDelayUntilNextTrigger()
	{
		DateTime now = DateTime.UtcNow;
		DateTime target = now.Date.Add(UtcTriggerTime.ToTimeSpan());

		if (target <= now)
		{
			target = target.AddDays(1);
		}

		return target - now;
	}
}
