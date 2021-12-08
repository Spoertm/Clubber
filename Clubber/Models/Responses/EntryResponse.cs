// Taken from devildaggers.info and modified
// Credit goes to Noah Stolk https://github.com/NoahStolk

namespace Clubber.Models.Responses
{
	public record struct EntryResponse
	(
		int Rank,
		int Id,
		string Username,
		int Time,
		int Kills,
		int Gems,
		int DeathType,
		int DaggersHit,
		int DaggersFired,
		ulong TimeTotal,
		ulong KillsTotal,
		ulong GemsTotal,
		ulong DeathsTotal,
		ulong DaggersHitTotal,
		ulong DaggersFiredTotal
	);
}
