using Clubber.Domain.Data.Entities;

namespace Clubber.Domain.Repositories;

public interface IPlayerPbRepository
{
    Task<PlayerPb?> GetByIdAsync(uint leaderboardId);

    Task UpsertAsync(PlayerPb playerPb);
}
