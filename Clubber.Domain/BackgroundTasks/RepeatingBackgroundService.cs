using Microsoft.Extensions.Hosting;
using Serilog;

namespace Clubber.Domain.BackgroundTasks;

public abstract class RepeatingBackgroundService : BackgroundService
{
	protected abstract TimeSpan TickInterval { get; }

	protected abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using PeriodicTimer timer = new(TickInterval);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				if (await timer.WaitForNextTickAsync(stoppingToken))
				{
					await ExecuteTaskAsync(stoppingToken);
				}
			}
			catch (OperationCanceledException operationCanceledException)
			{
				Log.Warning(operationCanceledException, "{ClassName} => service cancellation requested", nameof(RepeatingBackgroundService));
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Caught exception in {ClassName}", nameof(RepeatingBackgroundService));
			}
		}

		Log.Warning("{ClassName} => service cancelled", nameof(RepeatingBackgroundService));
	}
}
