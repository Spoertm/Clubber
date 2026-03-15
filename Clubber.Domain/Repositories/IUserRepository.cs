using Clubber.Domain.Models;

namespace Clubber.Domain.Repositories;

public interface IUserRepository
{
	Task<List<DdUser>> GetAllAsync();

	Task<List<DdUser>> GetByDiscordIdsAsync(IEnumerable<ulong> discordIds);

	Task<int> GetCountAsync();

	Task<DdUser?> FindAsync(int leaderboardId);

	Task<DdUser?> FindAsync(ulong discordId);

	Task<bool> TwitchUsernameExistsAsync(string twitchUsername);

	Task<Result> RegisterAsync(uint leaderboardId, ulong discordId);

	Task<Result> RegisterTwitchAsync(ulong discordId, string twitchUsername);

	Task<Result> UnregisterTwitchAsync(ulong discordId);

	Task<bool> RemoveAsync(ulong discordId);
}
