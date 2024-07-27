using System.ComponentModel.DataAnnotations;

namespace Clubber.Domain.Configuration;

public class AppConfig
{
	[Required]
	public string Prefix { get; set; } = "+";

	[Required]
	public string BotToken { get; set; } = string.Empty;

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong ClubberLoggerId { get; set; }

	[Required]
	public string ClubberLoggerToken { get; set; } = string.Empty;

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong RegisterChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong RoleAssignerRoleId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong CheaterRoleId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong DdPalsId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong UnregisteredRoleId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong DailyUpdateChannel { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong DailyUpdateLoggingChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong DdNewsChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong ModsChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong NoScoreRoleId { get; set; }
}
