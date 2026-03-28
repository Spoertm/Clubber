namespace Clubber.Domain.Data.Entities.DdSplits;

public sealed class HomingPeakRun
{
    public int Id { get; set; }

    public string PlayerName { get; set; } = null!;

    public int PlayerLeaderboardId { get; set; }

    public int HomingPeak { get; set; }

    public string Source { get; set; } = null!;
}
