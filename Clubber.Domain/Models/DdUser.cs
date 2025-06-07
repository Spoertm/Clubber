namespace Clubber.Domain.Models;

public sealed record DdUser
{
	public DdUser(ulong discordId, int leaderboardId, string? twitchUsername = null)
	{
		DiscordId = discordId;
		LeaderboardId = leaderboardId;
		TwitchUsername = twitchUsername;
	}

	public ulong DiscordId { get; init; }
	public int LeaderboardId { get; init; }
	public string? TwitchUsername { get; set; }
}
