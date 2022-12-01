using Clubber.Domain.Models.Responses;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clubber.Domain.Models.DdSplits;

public class BestSplit
{
	[Key]
	public string Name { get; set; } = null!;
	public int Time { get; set; }
	public int Value { get; set; }
	public string Description { get; set; } = null!;
	[Column(TypeName = "jsonb")]
	public GameInfo? GameInfo { get; set; }
}
