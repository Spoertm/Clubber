using Clubber.Database;
using Clubber.Files;
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
		private static readonly string _jsonDbFile = Path.Combine(AppContext.BaseDirectory, "Database", "UsersJson.json");
		public static List<DdUser> DdUsers => JsonConvert.DeserializeObject<List<DdUser>>(File.ReadAllText(_jsonDbFile));

		public static async Task RegisterUser(uint lbId, SocketGuildUser user)
		{
			dynamic lbPlayer = await GetLbPlayer(lbId);

			List<DdUser> list = DdUsers;
			list.Add(new DdUser(user.Id, (int)lbPlayer!.id));
			UpdateDbFile(list);
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

		public static bool RemoveUser(ulong discordId)
		{
			List<DdUser> list = DdUsers;
			DdUser? toRemove = list.Find(du => du.DiscordId == discordId);
			if (toRemove != null)
			{
				list.Remove(toRemove);
				UpdateDbFile(list);
				return true;
			}
			else
			{
				return false;
			}
		}

		private static void UpdateDbFile(List<DdUser> list) => File.WriteAllText(_jsonDbFile, JsonConvert.SerializeObject(list, Formatting.Indented));

		public static bool UserIsRegistered(ulong discordId) => DdUsers.Any(du => du.DiscordId == discordId);
	}
}
