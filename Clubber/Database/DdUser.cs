namespace Clubber.Files
{
	public class DdUser
	{
		public DdUser(ulong discordId, int leaderboardId, int score)
		{
			DiscordId = discordId;
			LeaderboardId = leaderboardId;
			Score = score;
		}

		public ulong DiscordId { get; set; }
		public int LeaderboardId { get; set; }
		public int Score { get; set; }
	}
}
