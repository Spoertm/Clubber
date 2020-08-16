namespace Clubber.DdRoleUpdater
{
    public class DdUser
    {
        public ulong DiscordId { get; set; }
        public int LeaderboardId { get; set; }
        public int? Score { get; set; }

        public DdUser(ulong discordId, int leaderboardId)
        {
            DiscordId = discordId;
            LeaderboardId = leaderboardId;
        }
    }
}
