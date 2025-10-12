using Serilog;

namespace Clubber.Domain.BackgroundTasks;

public sealed class KeepAppAliveService(IHttpClientFactory httpClientFactory) : RepeatingBackgroundService
{
	protected override TimeSpan TickInterval => TimeSpan.FromMinutes(5);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		const string envVarName = "AppUrls";
		if (Environment.GetEnvironmentVariable(envVarName) is not { } appUrlsRaw || string.IsNullOrWhiteSpace(appUrlsRaw))
		{
			Log.Warning("{EnvVarName} environment variable not set", envVarName);
			return;
		}

		string[] urls = appUrlsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (urls.Length == 0)
		{
			Log.Warning("No URLs found in {EnvVarName}", envVarName);
			return;
		}

		IEnumerable<Task> tasks = urls.Select(async url =>
		{
			try
			{
				using HttpClient client = httpClientFactory.CreateClient();
				await client.GetStringAsync(new Uri(url), stoppingToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				Log.Error(e, "Failed to ping {AppUrl}", url);
			}
		});

		await Task.WhenAll(tasks);
	}
}
