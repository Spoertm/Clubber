using Clubber.Models;
using Clubber.Models.Responses;
using Discord.WebSocket;

namespace Clubber.Helpers;

public interface IDatabaseHelper
{
	Task<List<DdUser>> GetEntireDatabase();

	Task<(bool Success, string Message)> RegisterUser(uint lbId, SocketGuildUser user);

	Task<(bool Success, string Message)> RegisterTwitch(ulong userId, string twitchUsername);

	Task<bool> RemoveUser(SocketGuildUser user);

	Task<bool> RemoveUser(ulong discordId);

	DdUser? GetDdUserBy(int lbId);

	DdUser? GetDdUserBy(ulong discordId);

	Task UpdateLeaderboardCache(List<EntryResponse> newEntries);

	Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth);

	Task CleanUpNewsItems();
}
