using Clubber.Models;
using Clubber.Models.Responses;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public interface IDatabaseHelper
	{
		List<DdUser> DdUserDatabase { get; }
		List<EntryResponse> LeaderboardCache { get; }

		Task<(bool Success, string Message)> RegisterUser(uint lbId, SocketGuildUser user);

		Task<bool> RemoveUser(SocketGuildUser user);

		Task<bool> RemoveUser(ulong discordId);

		DdUser? GetDdUserByDiscordId(ulong discordId);

		DdUser? GetDdUserByLbId(int lbId);

		Task UpdateLeaderboardCache(List<EntryResponse> newEntries);
	}
}
