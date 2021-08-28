using Clubber.Models;
using Clubber.Models.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public interface IWebService
	{
		Task<string> RequestStringAsync(string url);

		Task<List<LeaderboardUser>> GetLbPlayers(IEnumerable<uint> ids);

		Task<LeaderboardResponse> GetLeaderboardEntries(int rankStart);

		Task<string> GetCountryCodeForplayer(int lbId);
	}
}
