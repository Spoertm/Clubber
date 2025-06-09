using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Helpers;

public interface IDatabaseHelper
{
	Task<List<DdUser>> GetRegisteredUsers();

	Task<List<DdUser>> GetRegisteredUsers(IEnumerable<ulong> discordIds);

	Task<int> GetRegisteredUserCount();

	Task<Result> RegisterUser(uint lbId, ulong discordId);

	Task<Result> RegisterTwitch(ulong userId, string twitchUsername);

	Task<Result> UnregisterTwitch(ulong userId);

	Task<bool> RemoveUser(ulong discordId);

	Task<DdUser?> FindRegisteredUser(int lbId);

	Task<DdUser?> FindRegisteredUser(ulong discordId);

	Task UpdateLeaderboardCache(ICollection<EntryResponse> newEntries);

	Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth);

	Task CleanUpNewsItems();

	Task<bool> TwitchUsernameIsRegistered(string twitchUsername);

	Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsIfNeeded(
		IReadOnlyCollection<Split> splitsToBeChecked,
		DdStatsFullRunResponse ddstatsRun,
		string description);

	Task<BestSplit[]> GetBestSplits();

	Task<(HomingPeakRun? OldRun, HomingPeakRun? NewRun)> UpdateTopHomingPeaksIfNeeded(HomingPeakRun runToBeChecked);

	Task<HomingPeakRun[]> GetTopHomingPeaks();
}
