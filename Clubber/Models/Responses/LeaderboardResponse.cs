// Taken from devildaggers.info and modified
// Credit goes to Noah Stolk https://github.com/NoahStolk

namespace Clubber.Models.Responses;

public record struct LeaderboardResponse
{
	public LeaderboardResponse(DateTime dateTime, int totalPlayers, ulong timeGlobal, ulong killsGlobal, ulong gemsGlobal, ulong deathsGlobal, ulong daggersHitGlobal, ulong daggersFiredGlobal, ushort totalEntries) : this()
	{
		DateTime = dateTime;
		TotalPlayers = totalPlayers;
		TimeGlobal = timeGlobal;
		KillsGlobal = killsGlobal;
		GemsGlobal = gemsGlobal;
		DeathsGlobal = deathsGlobal;
		DaggersHitGlobal = daggersHitGlobal;
		DaggersFiredGlobal = daggersFiredGlobal;
		TotalEntries = totalEntries;
	}

	public DateTime DateTime { get; init; }

	public int TotalPlayers { get; set; }

	public ulong TimeGlobal { get; set; }

	public ulong KillsGlobal { get; set; }

	public ulong GemsGlobal { get; set; }

	public ulong DeathsGlobal { get; set; }

	public ulong DaggersHitGlobal { get; set; }

	public ulong DaggersFiredGlobal { get; set; }

	public ushort TotalEntries { get; set; }

	public List<EntryResponse> Entries { get; init; } = new();
}
