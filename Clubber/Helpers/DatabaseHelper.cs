using Clubber.Database;
using Clubber.Files;
using Clubber.Modules;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public static class DatabaseHelper
	{
		public static List<DdUser> DdUsers => JsonConvert.DeserializeObject<List<DdUser>>(File.ReadAllText(Program.DatabaseFile));

		public static async Task RegisterUser(uint lbId, SocketGuildUser user)
		{
			dynamic lbPlayer = await GetLbPlayer(lbId);

			List<DdUser> list = DdUsers;
			list.Add(new DdUser(user.Id, (int)lbPlayer!.id));
			await UpdateDbFile(list, $"Add-{user.Username}-{lbId}");
		}

		public static async Task<dynamic> GetLbPlayer(uint lbId)
		{
			using HttpClient client = new();
			try
			{
				string json = await client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
				return JsonConvert.DeserializeObject<dynamic>(json);
			}
			catch (JsonException jsonEx)
			{
				throw new CustomException("Something went wrong. Chupacabra will get on it soon:tm:.", jsonEx);
			}
			catch
			{
				throw new CustomException("DdInfo API issue. Please try again later.");
			}
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
			File.WriteAllText(Program.DatabaseFile, file);

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
