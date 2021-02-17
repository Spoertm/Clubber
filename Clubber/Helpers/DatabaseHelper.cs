using Clubber.Database;
using Clubber.Files;
using Clubber.Modules;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public static class DatabaseHelper
	{
		public static List<DdUser> DdUsers => JsonConvert.DeserializeObject<List<DdUser>>(File.ReadAllText(Directory.GetFiles(Program.DatabaseDirectory, "*.json")[0]));

		public static async Task RegisterUser(uint lbId, SocketGuildUser user)
		{
			LeaderboardUser lbPlayer = await GetLbPlayer(lbId);

			List<DdUser> list = DdUsers;
			list.Add(new DdUser(user.Id, lbPlayer.Id));
			await UpdateDbFile(list, $"Add-{user.Username}-{lbId}");
		}

		public static async Task<LeaderboardUser> GetLbPlayer(uint lbId)
		{
			using HttpClient client = new();
			try
			{
				List<KeyValuePair<string?, string?>> postValues = new()
				{
					new("uid", $"{lbId}"),
				};

				using FormUrlEncodedContent content = new(postValues);
				HttpResponseMessage response = await client.PostAsync("http://dd.hasmodai.com/backend16/get_user_by_id_public.php", content);
				byte[] data = await response.Content.ReadAsByteArrayAsync();

				int bytePosition = 19;

				return new LeaderboardUser(
					GetUserName(data, ref bytePosition),
					BitConverter.ToInt32(data, bytePosition),
					BitConverter.ToInt32(data, bytePosition + 4),
					BitConverter.ToInt32(data, bytePosition + 12),
					BitConverter.ToInt32(data, bytePosition + 16),
					BitConverter.ToInt32(data, bytePosition + 28),
					BitConverter.ToInt32(data, bytePosition + 24),
					BitConverter.ToInt32(data, bytePosition + 20),
					BitConverter.ToInt16(data, bytePosition + 32),
					BitConverter.ToUInt64(data, bytePosition + 60),
					BitConverter.ToUInt64(data, bytePosition + 44),
					BitConverter.ToUInt64(data, bytePosition + 68),
					BitConverter.ToUInt64(data, bytePosition + 36),
					BitConverter.ToUInt64(data, bytePosition + 76),
					BitConverter.ToUInt64(data, bytePosition + 52));
			}
			catch
			{
				throw new CustomException("DD servers are experiencing issues atm. Try again later.");
			}
		}

		public static string GetUserName(byte[] data, ref int bytePos)
		{
			short usernameLength = BitConverter.ToInt16(data, bytePos);
			bytePos += 2;

			byte[] usernameBytes = new byte[usernameLength];
			Buffer.BlockCopy(data, bytePos, usernameBytes, 0, usernameLength);

			bytePos += usernameLength;
			return Encoding.UTF8.GetString(usernameBytes);
		}

		public static async Task<bool> RemoveUser(SocketGuildUser user)
		{
			List<DdUser> list = DdUsers;
			DdUser? toRemove = list.Find(du => du.DiscordId == user.Id);
			if (toRemove != null)
			{
				list.Remove(toRemove);
				await UpdateDbFile(list, $"Remove-{user.Username}-{toRemove.LeaderboardId}");
				return true;
			}
			else
			{
				return false;
			}
		}

		public static async Task UpdateDbFile(List<DdUser> list, string change)
		{
			string file = JsonConvert.SerializeObject(list, Formatting.Indented);
			File.WriteAllText(Directory.GetFiles(Program.DatabaseDirectory, "*.json")[0], file);

			using (Stream stream = new MemoryStream())
			{
				StreamWriter writer = new StreamWriter(stream);
				writer.Write(file);
				writer.Flush();
				stream.Position = 0;
				await Info.BackupDbFile(stream, $"{DateTime.Now}--{change}.json");
				writer.Dispose();
			}
		}

		public static bool UserIsRegistered(ulong discordId) => DdUsers.Any(du => du.DiscordId == discordId);
	}
}
