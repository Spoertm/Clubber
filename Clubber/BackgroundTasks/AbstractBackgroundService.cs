using Serilog;

namespace Clubber.BackgroundTasks;

public abstract class AbstractBackgroundService : BackgroundService
{
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
				Log.Error(exception, "Caught exception in {}", nameof(AbstractBackgroundService));
			}

			if (Interval.TotalMilliseconds > 0)
				await Task.Delay(Interval, stoppingToken);
		}

		Log.Warning("{} => service cancelled", nameof(AbstractBackgroundService));
	}
}
