﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Clubber.Databases;
using Clubber.Files;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Clubber.Modules
{
	[Name("Database")]
	[Group("addfromlbid"), Alias("addlbid")]
	[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public class AddFromLbId : ModuleBase<SocketCommandContext>
	{
		private readonly IMongoCollection<DdUser> Database;

		public AddFromLbId(MongoDatabase mongoDatabase)
		{
			Database = mongoDatabase.DdUserCollection;
		}

		[Command]
		public async Task AddUserByID(uint lbId, [Remainder] string name)
		{
			string usernameornickname = name.ToLower();
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(usernameornickname) || (u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));
			await Helper.AddToDbFromName(userMatches, name, lbId, async (lbId, discordId) => await AddUserByLbIdAndDscId(lbId, discordId), Context.Channel);
		}

		[Command]
		public async Task AddUserByID(uint lbId, IUser userMention) => await AddUserByLbIdAndDscId(lbId, userMention.Id);

		[Command("id")]
		public async Task AddUserByLbIdAndDscId(uint lbId, ulong discordId)
		{
			try
			{
				HttpClient Client = new HttpClient();
				var user = Context.Guild.GetUser(discordId);
				if (Helper.DiscordIdExistsInDb(discordId, Database)) { await ReplyAsync($"User `{(user == null ? "" : user.Username)}({discordId})` is already registered."); return; }
				if (user == null) { await ReplyAsync($"❗ Could not find a user with the name or ID `{discordId}`."); return; }
				ulong cheaterRoleId = 693432614727581727;
				if (user.IsBot) { await ReplyAsync($"{user.Mention} is a bot. It can't be registered as a DD player."); return; }
				if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username} can't be registered because they've cheated."); return; }

				string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (Helper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId, Database)) { await ReplyAsync($"There already exists a user in the database with the leaderboard ID `{databaseUser.LeaderboardId}`."); return; }

				Database.InsertOne(databaseUser);
				await ReplyAsync($"✅ Added `{user.Username}` to the database.");
			}
			catch
			{ await ReplyAsync("❌ Couldn't execute command."); }
		}

	}
}
