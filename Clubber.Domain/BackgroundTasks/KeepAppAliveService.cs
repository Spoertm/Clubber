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
		const string appUrl = "https://clubber.onrender.com";
		await _httpClientFactory.CreateClient().GetStringAsync(appUrl, stoppingToken);
	}
}
