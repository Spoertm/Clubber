// Taken from devildaggers.info and modified
// Credit goes to Noah Stolk https://github.com/NoahStolk

namespace Clubber.Models.Responses;

public record struct LeaderboardResponse
{
	public LeaderboardResponse()
	{
		DateTime = default;
		TotalPlayers = 0;
		TimeGlobal = 0;
		KillsGlobal = 0;
		GemsGlobal = 0;
		DeathsGlobal = 0;
		DaggersHitGlobal = 0;
		DaggersFiredGlobal = 0;
		TotalEntries = 0;
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
