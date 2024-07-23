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
