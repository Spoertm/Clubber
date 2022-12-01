using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Services;

public interface IWebService
{
	Task<List<EntryResponse>> GetLbPlayers(IEnumerable<uint> ids);

	Task<List<EntryResponse>> GetSufficientLeaderboardEntries(int minimumScore);

	Task<string?> GetCountryCodeForplayer(int lbId);

	Task<DdStatsFullRunResponse> GetDdstatsResponse(string url);
}
