using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Clubber.Files
{
	public class LeaderboardData
	{
		public readonly List<int> Times = new List<int>();
		public readonly List<short> Kills = new List<short>();
		public readonly List<short> Gems = new List<short>();
		public readonly List<short> DaggersHit = new List<short>();
		public readonly List<int> DaggersFired = new List<int>();
		public readonly List<string> Deaths = new List<string>();

		public LeaderboardData(uint usersToRead = 229900)
		{
			string binaryLbPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Files/LB.bin");
			BinaryReader br = new BinaryReader(File.Open(binaryLbPath, FileMode.Open));

			Dictionary<byte, string> deathtypeDict = new Dictionary<byte, string>()
			{
				{ 0, "FALLEN" },
				{ 1, "SWARMED" },
				{ 2, "IMPALED" },
				{ 3, "GORED" },
				{ 4, "INFESTED" },
				{ 5, "OPENED" },
				{ 6, "PURGED" },
				{ 7, "DESECRATED" },
				{ 8, "SACRIFICED" },
				{ 9, "EVISCERATED" },
				{ 10, "ANNIHILATED" },
				{ 11, "INTOXICATED" },
				{ 12, "ENVENOMATED" },
				{ 13, "INCARNATED" },
				{ 14, "DISCARNATED" },
				{ 15, "BARBED" }
			};

			while (br.BaseStream.Position != 15 * usersToRead)
			{
				Times.Add((int)br.ReadUInt32());
				Kills.Add((short)br.ReadUInt16());
				Gems.Add((short)br.ReadUInt16());
				DaggersHit.Add((short)br.ReadUInt16());
				DaggersFired.Add((int)br.ReadUInt32());
				Deaths.Add(deathtypeDict[br.ReadByte()]);
			}
		}
	}
}
