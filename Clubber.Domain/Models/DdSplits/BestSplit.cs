﻿using Clubber.Domain.Models.Responses;
using System.ComponentModel.DataAnnotations;

namespace Clubber.Domain.Models.DdSplits;

public sealed class BestSplit
{
	[Key]
	public string Name { get; set; } = null!;
	public int Time { get; set; }
	public int Value { get; set; }
	public string Description { get; set; } = null!;
	public GameInfo? GameInfo { get; set; }
}
