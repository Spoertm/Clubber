using System.ComponentModel.DataAnnotations;
using Clubber.Domain.Models.Responses;

namespace Clubber.Domain.Data.Entities.DdSplits;

public sealed class BestSplit
{
    [Key]
    public string Name { get; set; } = null!;

    public int Time { get; set; }

    public int Value { get; set; }

    public string Description { get; set; } = null!;

    public GameInfo? GameInfo { get; set; }
}
