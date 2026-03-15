using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Repositories;

public interface ILeaderboardRepository
{
	Task<EntryResponse[]> GetCachedEntriesAsync();

	Task UpdateCacheAsync(ICollection<EntryResponse> entries);

	Task<BestSplit[]> GetBestSplitsAsync();

	Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsAsync(
		IReadOnlyCollection<Split> splits,
		DdStatsFullRunResponse run,
		string description);

	Task<HomingPeakRun[]> GetTopHomingPeaksAsync();

	Task<(HomingPeakRun? OldRun, HomingPeakRun? NewRun)> UpdateTopHomingPeakAsync(HomingPeakRun run);
}
