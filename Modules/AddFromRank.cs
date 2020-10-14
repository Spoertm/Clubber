using System.Collections.Generic;
using System;
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
	[Group("addfromrank"), Alias("addr")]
	[Summary("Obtains user from their rank and adds them to the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public class AddFromRank : ModuleBase<SocketCommandContext>
	{
		private readonly IMongoCollection<DdUser> Database;

		public AddFromRank(MongoDatabase mongoDatabase)
		{
			Database = mongoDatabase.DdUserCollection;
		}

		[Command]
		[Priority(1)]
		public async Task AddUserByRankAndName(uint rank, [Remainder] string name)
		{
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(
				u => u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				(u.Nickname != null && u.Nickname.Contains(name, StringComparison.InvariantCultureIgnoreCase)));
			await Helper.AddToDbFromName(userMatches, name, rank, async (rank, discordId) => await AddUserByRankAndId(rank, discordId), Context.Channel);
		}

		[Command]
		[Priority(2)]
		public async Task AddUserByRankAndMention(uint rank, IUser userMention) => await AddUserByRankAndId(rank, userMention.Id);

		[Command("id")]
		[Priority(3)]
		public async Task AddUserByRankAndId(uint rank, ulong discordId)
		{
			SocketGuildUser user = Context.Guild.GetUser(discordId);
			if (Helper.DiscordIdExistsInDb(discordId, Database)) { await ReplyAsync($"User `{(user == null ? "" : user.Username)}({discordId})` is already registered."); return; }
			if (user == null) { await ReplyAsync($"User not found."); return; }

			ulong cheaterRoleId = 693432614727581727;
			if (user.IsBot) { await ReplyAsync($"{user.Mention} is a bot. It can't be registered as a DD player."); return; }
			if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username} can't be registered because they've cheated."); return; }

			try
			{
				HttpClient client = new HttpClient();
				string jsonUser = await client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-rank?rank={rank}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (Helper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId, Database)) { await ReplyAsync($"There already exists a registered user with rank `{rank}` and leaderboard ID `{databaseUser.LeaderboardId}`."); return; }

				Database.InsertOne(databaseUser);
				await ReplyAsync($"✅ Added `{user.Username}` to the database.");
			}
			catch
			{
				await ReplyAsync("❌ Couldn't execute command.");
			}
		}
	}
}