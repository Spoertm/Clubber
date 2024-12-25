using System.ComponentModel.DataAnnotations;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Clubber.Domain.Configuration;

public class AppConfig
{
	[Required]
	public ConnectionStrings ConnectionStrings { get; set; }

	[Required]
	public DevilDaggersEndpoints DevilDaggersEndpoints { get; set; }

	[Required]
	public BotConfig BotConfig { get; set; }
}

public class ConnectionStrings
{
	[Required]
	public string DefaultConnection { get; set; }
}

public class DevilDaggersEndpoints
{
	[Required]
	[Url(ErrorMessage = "The value for 'GetMultipleUsersById' must be a valid URL.")]
	public string GetMultipleUsersById { get; set; }

	[Required]
	[Url(ErrorMessage = "The value for 'GetScores' must be a valid URL.")]
	public string GetScores { get; set; }
}

public class BotConfig
{
	[Required]
	public string Prefix { get; set; } = "+";

	[Required]
	public string BotToken { get; set; }

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
	public ulong DailyUpdateChannelId { get; set; }

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

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong NewPalRoleId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong PendingPbRoleId { get; set; }

	[Required]
	public IReadOnlyDictionary<int, ulong> ScoreRoles { get; set; }

	[Required]
	public IReadOnlyDictionary<int, ulong> RankRoles { get; set; }

	[Required]
	public IReadOnlyDictionary<int, string> DeathTypes { get; set; }
}
