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
	public class DatabaseHelper
	{
		private static List<DdUser> DdUsers => JsonConvert.DeserializeObject<List<DdUser>>(File.ReadAllText(JsonDbFile));

		private static string JsonDbFile => Path.Combine(AppContext.BaseDirectory, "UsersJson.json");

		public static async Task<string> RegisterUser(uint lbId, SocketGuildUser user)
		{
			if (DdUsers.Any(du => du.LeaderboardId == lbId))
				return $"User `{user.Username ?? string.Empty}({user.Id})` is already registered.";

			if (user.IsBot)
				return $"{user.Mention} is a bot. It can't be registered as a DD player.";

			if (user.Roles.Any(r => r.Id == Constants.CheaterRoleId))
				return $"{user.Username} can't be registered because they've cheated.";

			try
			{
				throw new HttpRequestException();
				using HttpClient client = new();
				string json = await client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
				dynamic lbPlayer = JsonConvert.DeserializeObject(json);
				AddUserToDatabase(new DdUser(user.Id, lbPlayer.Id, lbPlayer.Time / 10000));

				return "✅ Successfully registered";
			}
			catch
			{
				return "devildaggers.info is experiencing issues ATM. Please try again later.";
			}
		}

		public static void AddUserToDatabase(params DdUser[] users)
		{
			List<DdUser> list = DdUsers;
			list.AddRange(users);
			File.WriteAllText(JsonDbFile, JsonConvert.SerializeObject(list, Formatting.Indented));
		}
	}
}
