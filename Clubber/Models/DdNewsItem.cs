using Clubber.Models.Responses;

namespace Clubber.Models;

public record DdNewsItem(
	int Id,
	EntryResponse OldEntry,
	EntryResponse NewEntry,
	DateTime TimeOfOccurenceUtc,
	int Nth
);
