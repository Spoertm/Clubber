using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Clubber.Files
{
	public class LeaderboardData
	{
		public readonly List<uint> Times = new();
		public readonly List<ushort> Kills = new();
		public readonly List<ushort> Gems = new();
		public readonly List<ushort> DaggersHit = new();
		public readonly List<uint> DaggersFired = new();
		public readonly List<string> Deaths = new();

		public LeaderboardData()
		{
			Dictionary<byte, string> deathtypeDict = new()
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

			string binaryLbPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Path.Combine("Files", "LB.bin"));

			try
			{
				using BinaryReader br = new(File.Open(binaryLbPath, FileMode.Open));

				while (br.BaseStream.Position != br.BaseStream.Length)
				{
					Times.Add(br.ReadUInt32());
					Kills.Add(br.ReadUInt16());
					Gems.Add(br.ReadUInt16());
					DaggersHit.Add(br.ReadUInt16());
					DaggersFired.Add(br.ReadUInt32());
					Deaths.Add(deathtypeDict[br.ReadByte()]);
				}

				br.Close();
			}
			catch (IOException)
			{
				return;
			}
		}
	}
}
