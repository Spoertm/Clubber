using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;

namespace Clubber.Domain.Configuration;

public sealed class AppConfig
{
    public static FrozenDictionary<int, string> DeathTypes { get; } = new Dictionary<int, string>
    {
        [0] = "FALLEN",
        [1] = "SWARMED",
        [2] = "IMPALED",
        [3] = "GORED",
        [4] = "INFESTED",
        [5] = "OPENED",
        [6] = "PURGED",
        [7] = "DESECRATED",
        [8] = "SACRIFICED",
        [9] = "EVISCERATED",
        [10] = "ANNIHILATED",
        [11] = "INTOXICATED",
        [12] = "ENVENOMATED",
        [13] = "INCARNATED",
        [14] = "DISCARNATED",
        [15] = "ENTANGLED",
        [16] = "HAUNTED",
    }.ToFrozenDictionary();

    [Required]
    public string Prefix { get; set; } = "+";

    [Required]
    public required string BotToken { get; set; }

    [Required]
    public ulong FormerWrRoleId { get; set; }

    [Required]
    public ulong RegisterChannelId { get; set; }

    [Required]
    public ulong RoleAssignerRoleId { get; set; }

    [Required]
    public ulong CheaterRoleId { get; set; }

    [Required]
    public ulong DdPalsId { get; set; }

    [Required]
    public ulong UnregisteredRoleId { get; set; }

    [Required]
    public ulong DailyUpdateChannelId { get; set; }

    [Required]
    public ulong DailyUpdateLoggingChannelId { get; set; }

    [Required]
    public ulong DdNewsChannelId { get; set; }

    [Required]
    public ulong ModsChannelId { get; set; }

    [Required]
    public ulong NoScoreRoleId { get; set; }

    [Required]
    public ulong PendingPbRoleId { get; set; }

    [Required]
    public ulong NewPalRoleId { get; set; }

    [Required]
    public required Endpoints Endpoints { get; set; }

    public IReadOnlyCollection<ulong> BaseRoles => [
        FormerWrRoleId,
        UnregisteredRoleId,
        NoScoreRoleId,
        PendingPbRoleId
    ];
}
