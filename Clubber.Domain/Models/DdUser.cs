namespace Clubber.Domain.Models;

public record DdUser
{
	public DdUser(ulong discordId, uint lbId)
	{
		DiscordId = discordId;
		LeaderboardId = lbId;
	}

	public uint LeaderboardId { get; }

	public ulong DiscordId { get; }

	public string? TwitchUsername { get; set; }
}
