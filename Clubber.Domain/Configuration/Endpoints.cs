using System.ComponentModel.DataAnnotations;

namespace Clubber.Domain.Configuration;

public sealed class Endpoints
{
    [Required]
    public required Uri GetMultipleUsersById { get; set; }

    [Required]
    public required Uri GetScores { get; set; }

    [Required]
    public required Uri GetWorldRecords { get; set; }

    [Required]
    public required string GetCountryCodeForPlayer { get; set; }

    [Required]
    public required string GetPlayerHistory { get; set; }

    [Required]
    public required string GetDdstatsResponse { get; set; }
}
