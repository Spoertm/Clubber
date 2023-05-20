namespace Clubber.Domain.Models.Responses.DdInfo;

public record GetPlayerHistoryRankEntry
{
	public required DateTime DateTime { get; init; }

	public required int Rank { get; init; }
}
