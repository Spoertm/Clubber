using Newtonsoft.Json;

namespace Clubber.Domain.Models.Responses;

public class DdStatsFullRunResponse
{
	[JsonProperty("game_info")]
	public GameInfo GameInfo { get; set; } = null!;

	[JsonProperty("states")]
	public State[] States { get; set; } = null!;
}

public class State
{
	[JsonProperty("game_time")]
	public double GameTime { get; set; }

	[JsonProperty("gems")]
	public int Gems { get; set; }

	[JsonProperty("homing_daggers")]
	public int HomingDaggers { get; set; }

	[JsonProperty("daggers_hit")]
	public int DaggersHit { get; set; }

	[JsonProperty("daggers_fired")]
	public int DaggersFired { get; set; }

	[JsonProperty("accuracy")]
	public double Accuracy { get; set; }

	[JsonProperty("enemies_alive")]
	public int EnemiesAlive { get; set; }

	[JsonProperty("enemies_killed")]
	public int EnemiesKilled { get; set; }

	[JsonProperty("gems_despawned")]
	public int GemsDespawned { get; set; }
}

public class GameInfo
{
	[JsonProperty("player_name")]
	public string PlayerName { get; set; } = null!;

	[JsonProperty("id")]
	public int Id { get; set; }

	[JsonProperty("player_id")]
	public int PlayerId { get; set; }

	[JsonProperty("player_game_time")]
	public int PlayerGameTime { get; set; }

	[JsonProperty("granularity")]
	public int Granularity { get; set; }

	[JsonProperty("game_time")]
	public float GameTime { get; set; }

	[JsonProperty("death_type")]
	public string DeathType { get; set; } = null!;

	[JsonProperty("gems")]
	public int Gems { get; set; }

	[JsonProperty("homing_daggers")]
	public int HomingDaggers { get; set; }

	[JsonProperty("daggers_fired")]
	public int DaggersFired { get; set; }

	[JsonProperty("daggers_hit")]
	public int DaggersHit { get; set; }

	[JsonProperty("accuracy")]
	public double Accuracy { get; set; }

	[JsonProperty("enemies_alive")]
	public int EnemiesAlive { get; set; }

	[JsonProperty("enemies_killed")]
	public int EnemiesKilled { get; set; }

	[JsonProperty("time_stamp")]
	public DateTime TimeStamp { get; set; }

	[JsonProperty("replay_player_id")]
	public int ReplayPlayerId { get; set; }

	[JsonProperty("replay_player_name")]
	public string ReplayPlayerName { get; set; } = null!;

	[JsonProperty("spawnset")]
	public string Spawnset { get; set; } = null!;

	[JsonProperty("version")]
	public string Version { get; set; } = null!;

	[JsonProperty("level_two_time")]
	public double LevelTwoTime { get; set; }

	[JsonProperty("level_three_time")]
	public double LevelThreeTime { get; set; }

	[JsonProperty("level_four_time")]
	public double LevelFourTime { get; set; }

	[JsonProperty("levi_down_time")]
	public double LeviDownTime { get; set; }

	[JsonProperty("orb_down_time")]
	public double OrbDownTime { get; set; }

	[JsonProperty("homing_daggers_max_time")]
	public double HomingDaggersMaxTime { get; set; }

	[JsonProperty("enemies_alive_max_time")]
	public double EnemiesAliveMaxTime { get; set; }

	[JsonProperty("homing_daggers_max")]
	public int HomingDaggersMax { get; set; }

	[JsonProperty("enemies_alive_max")]
	public int EnemiesAliveMax { get; set; }

	[JsonProperty("is_replay")]
	public bool IsReplay { get; set; }
}