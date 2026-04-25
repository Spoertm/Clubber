namespace Clubber.Domain.Data.Entities;

public sealed class PlayerPb
{
    public uint LeaderboardId { get; set; }

    public string Username { get; set; } = null!;

    public int Time { get; set; }

    public int Rank { get; set; }

    public DateTimeOffset LastUpdated { get; set; }
}
