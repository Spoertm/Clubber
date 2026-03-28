namespace Clubber.Domain.Data.Entities;

public sealed record DdUser
{
    public DdUser(ulong discordId, uint leaderboardId, string? twitchUsername = null)
    {
        DiscordId = discordId;
        LeaderboardId = leaderboardId;
        TwitchUsername = twitchUsername;
    }

    public ulong DiscordId { get; init; }

    public uint LeaderboardId { get; init; }

    public string? TwitchUsername { get; set; }
}
