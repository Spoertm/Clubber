namespace Clubber.Domain.Models;

public record DdUser(ulong DiscordId, uint LeaderboardId, string? TwitchUsername = null)
{
	public string? TwitchUsername { get; set; } = TwitchUsername;
}
