namespace Clubber.Domain.Models;

public record DdUser
{
	public DdUser(ulong DiscordId, uint LeaderboardId, string? TwitchUsername = null)
	{
		this.DiscordId = DiscordId;
		this.LeaderboardId = LeaderboardId;
		this.TwitchUsername = TwitchUsername;
	}

	public ulong DiscordId { get; init; }
	public uint LeaderboardId { get; init; }
	public string? TwitchUsername { get; set; }
}
