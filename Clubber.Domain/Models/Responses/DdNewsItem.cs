namespace Clubber.Domain.Models.Responses;

public sealed record DdNewsItem(
	int LeaderboardId,
	EntryResponse OldEntry,
	EntryResponse NewEntry,
	DateTimeOffset TimeOfOccurenceUtc,
	int Nth)
{
	public int ItemId { get; init; }
}
