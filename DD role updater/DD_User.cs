namespace Clubber.DDroleupdater
{
    public class DD_User
    {
        public ulong DiscordID { get; set; }
        public int LeaderboardID { get; set; }
        public int Score { get; set; }
        public ulong RoleID { get; set; }

        public DD_User(ulong discordID, int leaderboardID)
        {
            DiscordID = discordID;
            LeaderboardID = leaderboardID;
        }
    }
}
