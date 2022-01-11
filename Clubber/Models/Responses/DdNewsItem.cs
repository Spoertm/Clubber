namespace Clubber.Models.Responses;

public record DdNewsItem(
	int LeaderboardId,
	EntryResponse OldEntry,
	EntryResponse NewEntry,
	DateTime TimeOfOccurenceUtc,
	int Nth,
	int ItemId = 0
);
