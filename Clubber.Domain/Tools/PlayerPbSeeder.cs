using Clubber.Domain.Data.Entities;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Serilog;

namespace Clubber.Domain.Tools;

/// <summary>
/// One-time seeder to populate PlayerPbs and HundredthCounts tables from registered DdUsers.
/// Fetches current PB data from the API for all registered users.
/// </summary>
public sealed class PlayerPbSeeder(
    IUserRepository userRepository,
    IPlayerPbRepository playerPbRepository,
    IHundredthCountRepository hundredthCountRepository,
    IWebService webService)
{
    private const int BatchSize = 100;

    public async Task SeedAsync(bool seedHundredthCounts = true)
    {
        Log.Information("Starting PlayerPb seeding...");

        List<DdUser> users = await userRepository.GetAllAsync();
        if (users.Count == 0)
        {
            Log.Warning("No registered users found to seed.");
            return;
        }

        Log.Information("Found {Count} registered users. Fetching PB data in batches of {BatchSize}...", users.Count, BatchSize);

        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < users.Count; i += BatchSize)
        {
            List<DdUser> batch = [.. users.Skip(i).Take(BatchSize)];
            uint[] ids = [.. batch.Select(u => u.LeaderboardId)];

            try
            {
                IReadOnlyList<EntryResponse> players = await webService.GetLbPlayers(ids);
                Dictionary<uint, EntryResponse> playerLookup = players.ToDictionary(p => p.Id);

                foreach (DdUser user in batch)
                {
                    if (playerLookup.TryGetValue(user.LeaderboardId, out EntryResponse? player))
                    {
                        await playerPbRepository.UpsertAsync(new PlayerPb
                        {
                            LeaderboardId = player.Id,
                            Username = player.Username,
                            Time = player.Time,
                            Rank = player.Rank,
                            LastUpdated = DateTimeOffset.UtcNow,
                        });

                        successCount++;
                    }
                    else
                    {
                        Log.Warning("Player {LeaderboardId} not found in API response", user.LeaderboardId);
                        failCount++;
                    }
                }

                Log.Information(
                    "Processed batch {Current}/{Total} ({Success} succeeded, {Failed} failed)",
                    (i / BatchSize) + 1,
                    (int)Math.Ceiling(users.Count / (double)BatchSize),
                    successCount,
                    failCount);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to fetch batch starting at index {Index}", i);
                failCount += batch.Count;
            }
        }

        Log.Information("PlayerPb seeding complete. Success: {Success}, Failed: {Failed}", successCount, failCount);

        if (seedHundredthCounts && successCount > 0)
        {
            Log.Information("Seeding HundredthCounts from PlayerPbs...");
            await hundredthCountRepository.SeedFromPlayerPbsAsync();
            Log.Information("HundredthCounts seeding complete.");
        }
    }
}
