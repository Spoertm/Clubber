using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;

namespace Clubber.Domain.Services;

public interface IWebService
{
	Task<IReadOnlyList<EntryResponse>> GetLbPlayers(IEnumerable<uint> ids);

	Task<ICollection<EntryResponse>> GetSufficientLeaderboardEntries(int minimumScore);

	Task<string?> GetCountryCodeForplayer(int lbId);

	Task<GetPlayerHistory?> GetPlayerHistory(int lbId);

	Task<DdStatsFullRunResponse> GetDdstatsResponse(Uri uri);
}
