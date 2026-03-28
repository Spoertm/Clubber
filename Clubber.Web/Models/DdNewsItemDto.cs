using Clubber.Domain.Models.Responses;

namespace Clubber.Web.Models;

internal readonly record struct DdNewsItemDto(
    uint LeaderboardId,
    EntryResponse OldEntry,
    EntryResponse NewEntry,
    DateTimeOffset TimeOfOccurenceUtc,
    int Nth);
