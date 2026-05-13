using Clubber.Domain.Data;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;

namespace Clubber.Web;

/// <summary>
/// Seeder for populating PlayerPb table from the official leaderboard.
/// Seeds all players from rank 1 down to the minimum trackable score (700s).
/// </summary>
internal static class PlayerPbSeeder
{
    private const int MinimumTrackableScore = 700;

    public static async Task SeedAsync(IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        IWebService webService = scope.ServiceProvider.GetRequiredService<IWebService>();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        int offset = 0;
        int seededCount = 0;
        int threshold = MinimumTrackableScore * 10_000;

        while (true)
        {
            IReadOnlyList<EntryResponse> entries = await webService.GetLeaderboardScores(offset);

            if (entries.Count == 0)
            {
                break;
            }

            bool belowThreshold = false;
            foreach (EntryResponse entry in entries)
            {
                if (entry.Time < threshold)
                {
                    belowThreshold = true;
                    break;
                }

                PlayerPb? existing = await dbContext.PlayerPbs.FindAsync(entry.Id);
                if (existing is null)
                {
                    dbContext.PlayerPbs.Add(new PlayerPb
                    {
                        LeaderboardId = entry.Id,
                        Username = entry.Username,
                        Time = entry.Time,
                        Rank = entry.Rank,
                        LastUpdated = DateTimeOffset.UtcNow,
                    });
                }
                else
                {
                    existing.Username = entry.Username;
                    existing.Time = entry.Time;
                    existing.Rank = entry.Rank;
                    existing.LastUpdated = DateTimeOffset.UtcNow;
                }

                seededCount++;
            }

            if (belowThreshold)
            {
                break;
            }

            offset += entries.Count;

            await Task.Delay(500); // Be nice to the API and avoid hitting rate limits
        }

        await dbContext.SaveChangesAsync();
        Serilog.Log.Information("Seeded {Count} PlayerPb records from rank 1 down to {MinimumScore}s.", seededCount, MinimumTrackableScore);
    }
}
