using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Helpers;

public interface IDatabaseHelper
{
	Task<List<DdUser>> GetEntireDatabase();

	Task<Result> RegisterUser(uint lbId, ulong discordId);

	Task<Result> RegisterTwitch(ulong userId, string twitchUsername);

	Task<Result> UnregisterTwitch(ulong userId);

	Task<bool> RemoveUser(ulong discordId);

	Task<DdUser?> GetDdUserBy(int lbId);

	Task<DdUser?> GetDdUserBy(ulong discordId);

	Task UpdateLeaderboardCache(ICollection<EntryResponse> newEntries);

	Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth);

	Task CleanUpNewsItems();

	Task<bool> TwitchUsernameIsRegistered(string twitchUsername);

	Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsIfNeeded(Split[] splitsToBeChecked, DdStatsFullRunResponse ddstatsRun, string description);

	Task<BestSplit[]> GetBestSplits();

	Task<(HomingPeakRun? OldRun, HomingPeakRun? NewRun)> UpdateTopHomingPeaksIfNeeded(HomingPeakRun runToBeChecked);

	Task<HomingPeakRun[]> GetTopHomingPeaks();
}
