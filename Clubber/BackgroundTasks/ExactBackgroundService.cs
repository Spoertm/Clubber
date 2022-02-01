using Serilog;

namespace Clubber.BackgroundTasks;

public abstract class ExactBackgroundService : BackgroundService
{
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
				Log.Error(exception, "Caught exception in {}", nameof(ExactBackgroundService));
			}

			await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
		}

		Log.Warning("{} => service cancelled", nameof(ExactBackgroundService));
	}
}
