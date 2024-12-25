namespace Clubber.Domain.Models.Responses;

public class DdNewsEntryResponse
{
	public int Rank { get; set; }
	public int Id { get; set; }
	public string Username { get; set; } = null!;
	public int Time { get; set; }
	public int Kills { get; set; }
	public int Gems { get; set; }
	public int DeathType { get; set; }
	public int DaggersHit { get; set; }
	public int DaggersFired { get; set; }
	public ulong TimeTotal { get; set; }
	public ulong KillsTotal { get; set; }
	public ulong GemsTotal { get; set; }
	public ulong DeathsTotal { get; set; }
	public ulong DaggersHitTotal { get; set; }
	public ulong DaggersFiredTotal { get; set; }

	public static implicit operator DdNewsEntryResponse(EntryResponse entryResponse)
	{
		return new()
		{
			Rank = entryResponse.Rank,
			Id = entryResponse.Id,
			Username = entryResponse.Username,
			Time = entryResponse.Time,
			Kills = entryResponse.Kills,
			Gems = entryResponse.Gems,
			DeathType = entryResponse.DeathType,
			DaggersHit = entryResponse.DaggersHit,
			DaggersFired = entryResponse.DaggersFired,
			TimeTotal = entryResponse.TimeTotal,
			KillsTotal = entryResponse.KillsTotal,
			GemsTotal = entryResponse.GemsTotal,
			DeathsTotal = entryResponse.DeathsTotal,
			DaggersHitTotal = entryResponse.DaggersHitTotal,
			DaggersFiredTotal = entryResponse.DaggersFiredTotal,
		};
	}
}
