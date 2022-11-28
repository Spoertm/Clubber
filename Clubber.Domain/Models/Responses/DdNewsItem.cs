namespace Clubber.Domain.Models.Responses;

public record DdNewsItem(
	int LeaderboardId,
	EntryResponse OldEntry,
	EntryResponse NewEntry,
	DateTimeOffset TimeOfOccurenceUtc,
	int Nth,
	int ItemId = 0
);
