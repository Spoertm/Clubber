using System.Text.Json.Serialization;

namespace Clubber.Domain.Models.Responses;

public record GetRecentResponse
{
    [JsonPropertyName("uid")]
    public int Uid { get; init; }

    [JsonPropertyName("kills")]
    public int Kills { get; init; }

    [JsonPropertyName("daggers")]
    public int Daggers { get; init; }

    [JsonPropertyName("time")]
    public int Time { get; init; }

    [JsonPropertyName("hits")]
    public int Hits { get; init; }

    [JsonPropertyName("gems")]
    public int Gems { get; init; }

    [JsonPropertyName("death")]
    public int Death { get; init; }

    [JsonPropertyName("user_uid")]
    public uint LeaderboardId { get; init; }

    [JsonPropertyName("country")]
    public required string Country { get; init; }

    [JsonPropertyName("user_name")]
    public required string UserName { get; init; }

    [JsonIgnore]
    public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeSeconds(TimestampUnix);

    [JsonInclude]
    [JsonPropertyName("timestamp")]
    private long TimestampUnix { get; init; }
}
