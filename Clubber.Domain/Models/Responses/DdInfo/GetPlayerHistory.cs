namespace Clubber.Domain.Models.Responses.DdInfo;

public record GetPlayerHistory
{
	public required int? BestRank { get; init; }

	public required bool HidePastUsernames { get; init; }

	public required IReadOnlyList<string> Usernames { get; init; }

	public required IReadOnlyList<GetPlayerHistoryScoreEntry> ScoreHistory { get; init; }

	public required IReadOnlyList<GetPlayerHistoryActivityEntry> ActivityHistory { get; init; }

	public required IReadOnlyList<GetPlayerHistoryRankEntry> RankHistory { get; init; }
}
