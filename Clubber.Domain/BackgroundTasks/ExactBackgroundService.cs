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
			await ExecuteIfOnTimeAsync(stoppingToken);
			await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
		}

		Log.Warning("{} => service cancelled", nameof(ExactBackgroundService));
	}

	private async Task ExecuteIfOnTimeAsync(CancellationToken stoppingToken)
	{
		if (DateTime.UtcNow.Minute != UtcTriggerTime.Minute)
		{
			return;
		}

		try
		{
			await ExecuteTaskAsync(stoppingToken);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Caught exception in {}", nameof(ExactBackgroundService));
		}
	}
}
