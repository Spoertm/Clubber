namespace Clubber.Models
{
	public record struct LeaderboardUser(
		string Username,
		int Rank,
		int Id,
		int Time,
		int Kills,
		int Gems,
		int DaggersHit,
		int DaggersFired,
		short DeathType,
		ulong TimeTotal,
		ulong KillsTotal,
		ulong GemsTotal,
		ulong DeathsTotal,
		ulong DaggersHitTotal,
		ulong DaggersFiredTotal);
}
