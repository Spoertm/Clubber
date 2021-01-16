using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Clubber.Files
{
	public class LeaderboardData
	{
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
				{ 15, "BARBED" },
			};

			string binaryLbPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Files/LB.bin");

			try
			{
				BinaryReader br = new BinaryReader(File.Open(binaryLbPath, FileMode.Open));

				while (br.BaseStream.Position != br.BaseStream.Length)
				{
					PlayerList.Add(new LbDataPlayer(
						(float)br.ReadUInt32() / 10000,
						br.ReadUInt16(),
						br.ReadUInt16(),
						br.ReadUInt16(),
						(int)br.ReadUInt32(),
						deathtypeDict[br.ReadByte()]));
				}

				br.Close();
			}
			catch (IOException)
			{
				// Ignore IO exceptions.
			}
		}

		public List<LbDataPlayer> PlayerList { get; } = new();
	}
}