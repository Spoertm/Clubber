﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clubber.Domain.Models.Database;

[Keyless]
public class ClubberDbConfig
{
	[Column(TypeName = "jsonb")]
	public string JsonConfig { get; set; } = null!;
}
