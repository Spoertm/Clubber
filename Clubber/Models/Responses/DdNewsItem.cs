namespace Clubber.Models.Responses;

public record DdNewsItem(
	int Id,
	EntryResponse OldEntry,
	EntryResponse NewEntry,
	DateTime TimeOfOccurenceUtc,
	int Nth
);
