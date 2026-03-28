using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Data.Entities;

public sealed class DdNewsItem
{
    public int ItemId { get; init; }

    public required uint LeaderboardId { get; set; }

    public required EntryResponse OldEntry { get; set; }

    public required EntryResponse NewEntry { get; set; }

    public required DateTimeOffset TimeOfOccurenceUtc { get; set; }

    public required int Nth { get; set; }
}
