using Microsoft.Extensions.Hosting;
using Serilog;

namespace Clubber.Domain.BackgroundTasks;

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

				if (Interval.TotalMilliseconds > 0)
				{
					await Task.Delay(Interval, stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
				Log.Warning("{ClassName} => service cancellation requested", GetType().Name);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Caught exception in {ClassName}", GetType().Name);
			}
		}

		Log.Warning("{ClassName} => service cancelled", GetType().Name);
	}
}
