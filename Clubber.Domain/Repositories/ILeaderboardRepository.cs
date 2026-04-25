using Clubber.Domain.Data.Entities.DdSplits;
using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Repositories;

public interface ILeaderboardRepository
{
    Task<BestSplit[]> GetBestSplitsAsync();

    Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsAsync(
        IReadOnlyCollection<Split> splits,
        DdStatsFullRunResponse run,
        string description);

    Task<HomingPeakRun[]> GetTopHomingPeaksAsync();

    Task<(HomingPeakRun? OldRun, HomingPeakRun? NewRun)> UpdateTopHomingPeakAsync(HomingPeakRun run);
}
