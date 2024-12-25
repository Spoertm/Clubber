namespace Clubber.Domain.Models.Responses;

public record DdNewsItem
{
	public int LeaderboardId { get; init; }
	public DdNewsEntryResponse OldEntry { get; init; }
	public DdNewsEntryResponse NewEntry { get; init; }
	public DateTimeOffset TimeOfOccurenceUtc { get; init; }
	public int Nth { get; init; }
	public int ItemId { get; init; }

	// Parameterless constructor for EF Core
	public DdNewsItem() { }

	public DdNewsItem(int leaderboardId, DdNewsEntryResponse oldEntry, DdNewsEntryResponse newEntry, DateTimeOffset timeOfOccurenceUtc, int nth, int itemId = 0)
	{
		LeaderboardId = leaderboardId;
		OldEntry = oldEntry;
		NewEntry = newEntry;
		TimeOfOccurenceUtc = timeOfOccurenceUtc;
		Nth = nth;
		ItemId = itemId;
	}
}
