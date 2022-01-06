namespace Clubber.Models;

public record DdUser(ulong DiscordId, int LeaderboardId, string? TwitchUsername = null)
{
	public string? TwitchUsername { get; set; } = TwitchUsername;
}
