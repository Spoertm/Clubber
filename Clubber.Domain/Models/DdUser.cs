namespace Clubber.Domain.Models;

public sealed record DdUser
{
	public DdUser(ulong DiscordId, int LeaderboardId, string? TwitchUsername = null)
	{
		this.DiscordId = DiscordId;
		this.LeaderboardId = LeaderboardId;
		this.TwitchUsername = TwitchUsername;
	}

	public ulong DiscordId { get; init; }
	public int LeaderboardId { get; init; }
	public string? TwitchUsername { get; set; }
}
