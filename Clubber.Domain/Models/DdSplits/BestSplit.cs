using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Models.DdSplits;

public class BestSplit
{
	public string Name { get; set; } = null!;
	public int Time { get; set; }
	public int Value { get; set; }
	public string Description { get; set; } = null!;
	public GameInfo? GameInfo { get; set; }
}
