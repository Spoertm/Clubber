namespace Clubber.Files
{
	public class DdUser
	{
		public DdUser(ulong discordId, int leaderboardId)
		{
			DiscordId = discordId;
			LeaderboardId = leaderboardId;
		}

		public ulong DiscordId { get; set; }
		public int LeaderboardId { get; set; }
	}
}
