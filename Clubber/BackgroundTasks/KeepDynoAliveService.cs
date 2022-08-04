namespace Clubber.BackgroundTasks;

public class KeepDynoAliveService : AbstractBackgroundService
{
	private readonly IHttpClientFactory _httpClientFactory;

	public KeepDynoAliveService(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	protected override TimeSpan Interval => TimeSpan.FromMinutes(5);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		const string clubberHerokuUrl = "https://clubberbot.herokuapp.com/";
		await _httpClientFactory.CreateClient().GetStringAsync(clubberHerokuUrl);
	}
}
