using Clubber.Domain.Data;
using Clubber.Domain.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Domain.Repositories;

public sealed class PlayerPbRepository(AppDbContext dbContext) : IPlayerPbRepository
{
    public async Task<PlayerPb?> GetByIdAsync(uint leaderboardId)
    {
        return await dbContext.PlayerPbs.AsNoTracking().FirstOrDefaultAsync(p => p.LeaderboardId == leaderboardId);
    }

    public async Task UpsertAsync(PlayerPb playerPb)
    {
        PlayerPb? existing = await dbContext.PlayerPbs.FindAsync(playerPb.LeaderboardId);
        if (existing is null)
        {
            dbContext.PlayerPbs.Add(playerPb);
        }
        else
        {
            existing.Username = playerPb.Username;
            existing.Time = playerPb.Time;
            existing.Rank = playerPb.Rank;
            existing.LastUpdated = playerPb.LastUpdated;
        }

        await dbContext.SaveChangesAsync();
    }
}
