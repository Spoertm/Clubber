namespace Clubber.Files
{
	public class LbDataPlayer
	{
		public float Time;
		public int Kills;
		public int Gems;
		public int DaggersHit;
		public int DaggersFired;
		public string Death;

		public LbDataPlayer(float time, int kills, int gems, int daggersHit, int daggersFired, string death)
		{
			Time = time;
			Kills = kills;
			Gems = gems;
			DaggersHit = daggersHit;
			DaggersFired = daggersFired;
			Death = death;
		}
	}
}
