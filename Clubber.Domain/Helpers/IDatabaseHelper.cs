using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Discord.WebSocket;

namespace Clubber.Domain.Helpers;

public interface IDatabaseHelper
{
	Task<List<DdUser>> GetEntireDatabase();

	Task<Result> RegisterUser(uint lbId, SocketGuildUser user);

	Task<Result> RegisterTwitch(ulong userId, string twitchUsername);

	Task<Result> UnregisterTwitch(ulong userId);

	Task<bool> RemoveUser(SocketGuildUser user);

	Task<bool> RemoveUser(ulong discordId);

	DdUser? GetDdUserBy(int lbId);

	DdUser? GetDdUserBy(ulong discordId);

	Task UpdateLeaderboardCache(List<EntryResponse> newEntries);

	Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth);

	Task CleanUpNewsItems();

	Task<bool> TwitchUsernameIsRegistered(string twitchUsername);

	Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsIfNeeded(Split[] splitsToBeChecked, DdStatsFullRunResponse ddstatsRun, string description);

	Task<BestSplit[]> GetBestSplits();

	Task<(HomingPeakRun[] OldTopPeaks, HomingPeakRun? NewPeakRun)> UpdateTopHomingPeaksIfNeeded(HomingPeakRun runToBeChecked);

	Task<HomingPeakRun[]> GetTopHomingPeaks();
}
