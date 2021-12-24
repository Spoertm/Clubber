using Clubber.Models.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public interface IWebService
	{
		Task<List<EntryResponse>> GetLbPlayers(IEnumerable<uint> ids);

		Task<List<EntryResponse>> GetSufficientLeaderboardEntries(int minimumScore);

		Task<string> GetCountryCodeForplayer(int lbId);
	}
}
