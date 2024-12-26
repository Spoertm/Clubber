namespace Clubber.Domain.Models.Responses;

public record DdNewsItem
{
	public required int LeaderboardId { get; init; }
	public required DdNewsEntryResponse OldEntry { get; init; }
	public required DdNewsEntryResponse NewEntry { get; init; }
	public required DateTimeOffset TimeOfOccurenceUtc { get; init; }
	public required int Nth { get; init; }
	public int ItemId { get; init; }
}
