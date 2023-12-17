using Serilog;

namespace Clubber.Domain.BackgroundTasks;

public class KeepAppAliveService : AbstractBackgroundService
{
	private readonly IHttpClientFactory _httpClientFactory;

	public KeepAppAliveService(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	protected override TimeSpan Interval => TimeSpan.FromMinutes(5);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		const string envVarName = "AppUrl";
		if (Environment.GetEnvironmentVariable(envVarName) is not { } appUrl)
		{
			Log.Warning("{EnvVarName} environment variable not set", envVarName);
			return;
		}

		try
		{
			await _httpClientFactory.CreateClient().GetStringAsync(appUrl, stoppingToken).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			Log.Error(e, "Failed to ping {AppUrl}", appUrl);
		}
	}
}
