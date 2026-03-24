using Serilog;

namespace Clubber.Domain.BackgroundTasks;

public sealed class KeepAppAliveService(IHttpClientFactory httpClientFactory) : RepeatingBackgroundService
{
    private const string EnvVarName = "AppUrls";

    protected override TimeSpan TickInterval => TimeSpan.FromMinutes(5);

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        if (Environment.GetEnvironmentVariable(EnvVarName) is not { } appUrlsRaw || string.IsNullOrWhiteSpace(appUrlsRaw))
        {
            Log.Warning("{EnvVarName} environment variable not set", EnvVarName);
            return;
        }

        string[] urls = appUrlsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (urls.Length == 0)
        {
            Log.Warning("No URLs found in {EnvVarName}", EnvVarName);
            return;
        }

        using HttpClient client = httpClientFactory.CreateClient(nameof(KeepAppAliveService));

        IEnumerable<Task> tasks = urls.Select(async url =>
        {
            try
            {
                _ = await client.GetStringAsync(new Uri(url), stoppingToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to ping {AppUrl}", url);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
