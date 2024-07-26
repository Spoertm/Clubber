using System.Text.Json.Serialization;

namespace Clubber.Domain.Models.Responses;

public class DdStatsFullRunResponse
{
	[JsonPropertyName("game_info")]
	public GameInfo GameInfo { get; set; } = null!;

	[JsonPropertyName("states")]
	public IReadOnlyList<State> States { get; set; } = null!;
}

public class State
{
	[JsonPropertyName("game_time")]
	public double GameTime { get; set; }

	[JsonPropertyName("gems")]
	public int Gems { get; set; }

	[JsonPropertyName("homing_daggers")]
	public int HomingDaggers { get; set; }

	[JsonPropertyName("daggers_hit")]
	public int DaggersHit { get; set; }

	[JsonPropertyName("daggers_fired")]
	public int DaggersFired { get; set; }

	[JsonPropertyName("accuracy")]
	public double Accuracy { get; set; }

	[JsonPropertyName("enemies_alive")]
	public int EnemiesAlive { get; set; }

	[JsonPropertyName("enemies_killed")]
	public int EnemiesKilled { get; set; }

	[JsonPropertyName("gems_despawned")]
	public int GemsDespawned { get; set; }
}

public class GameInfo
{
	[JsonPropertyName("player_name")]
	public string PlayerName { get; set; } = null!;

	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("player_id")]
	public int PlayerId { get; set; }

	[JsonPropertyName("player_game_time")]
	public int PlayerGameTime { get; set; }

	[JsonPropertyName("granularity")]
	public int Granularity { get; set; }

	[JsonPropertyName("game_time")]
	public float GameTime { get; set; }

	[JsonPropertyName("death_type")]
	public string DeathType { get; set; } = null!;

	[JsonPropertyName("gems")]
	public int Gems { get; set; }

	[JsonPropertyName("homing_daggers")]
	public int HomingDaggers { get; set; }

	[JsonPropertyName("daggers_fired")]
	public int DaggersFired { get; set; }

	[JsonPropertyName("daggers_hit")]
	public int DaggersHit { get; set; }

	[JsonPropertyName("accuracy")]
	public double Accuracy { get; set; }

	[JsonPropertyName("enemies_alive")]
	public int EnemiesAlive { get; set; }

	[JsonPropertyName("enemies_killed")]
	public int EnemiesKilled { get; set; }

	[JsonPropertyName("time_stamp")]
	public DateTime TimeStamp { get; set; }

	[JsonPropertyName("replay_player_id")]
	public int ReplayPlayerId { get; set; }

	[JsonPropertyName("replay_player_name")]
	public string ReplayPlayerName { get; set; } = null!;

	[JsonPropertyName("spawnset")]
	public string Spawnset { get; set; } = null!;

	[JsonPropertyName("version")]
	public string Version { get; set; } = null!;

	[JsonPropertyName("level_two_time")]
	public double LevelTwoTime { get; set; }

	[JsonPropertyName("level_three_time")]
	public double LevelThreeTime { get; set; }

	[JsonPropertyName("level_four_time")]
	public double LevelFourTime { get; set; }

	[JsonPropertyName("levi_down_time")]
	public double LeviDownTime { get; set; }

	[JsonPropertyName("orb_down_time")]
	public double OrbDownTime { get; set; }

	[JsonPropertyName("homing_daggers_max_time")]
	public double HomingDaggersMaxTime { get; set; }

	[JsonPropertyName("enemies_alive_max_time")]
	public double EnemiesAliveMaxTime { get; set; }

	[JsonPropertyName("homing_daggers_max")]
	public int HomingDaggersMax { get; set; }

	[JsonPropertyName("enemies_alive_max")]
	public int EnemiesAliveMax { get; set; }

	[JsonPropertyName("is_replay")]
	public bool IsReplay { get; set; }

	[JsonIgnore]
	public Uri Url => new($"https://ddstats.com/games/{Id}");
}
