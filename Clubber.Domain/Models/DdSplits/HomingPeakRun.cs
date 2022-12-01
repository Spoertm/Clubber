using System.ComponentModel.DataAnnotations;

namespace Clubber.Domain.Models.DdSplits;

public class HomingPeakRun
{
	[Key]
	public int Id { get; set; } = 0;
	public string PlayerName { get; set; } = null!;
	public int PlayerLeaderboardId { get; set; }
	public int HomingPeak { get; set; }
	public string Source { get; set; } = null!;
}
