using Clubber.Databases;
using Clubber.Files;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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
		[Priority(1)]
		public async Task AddUserByID(uint lbId, [Remainder] string name)
		{
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u =>
			u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
			(u.Nickname != null && u.Nickname.Contains(name, StringComparison.InvariantCultureIgnoreCase)));
			await Helper.AddToDbFromName(userMatches, name, lbId, async (lbId, discordId) => await AddUserByLbIdAndDscId(lbId, discordId), Context.Channel);
		}

		[Command]
		[Priority(2)]
		public async Task AddUserByID(uint lbId, IUser userMention) => await AddUserByLbIdAndDscId(lbId, userMention.Id);

		[Command("id")]
		[Priority(3)]
		public async Task AddUserByLbIdAndDscId(uint lbId, ulong discordId)
		{
			SocketGuildUser user = Context.Guild.GetUser(discordId);
			if (Helper.DiscordIdExistsInDb(discordId, Database)) { await ReplyAsync($"User `{(user == null ? "" : user.Username)}({discordId})` is already registered."); return; }
			if (user == null) { await ReplyAsync($"User not found."); return; }

			const ulong cheaterRoleId = 693432614727581727;
			if (user.IsBot) { await ReplyAsync($"{user.Mention} is a bot. It can't be registered as a DD player."); return; }
			if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username} can't be registered because they've cheated."); return; }

			try
			{
				HttpClient client = new HttpClient();
				string jsonUser = await client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (Helper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId, Database)) { await ReplyAsync($"There already exists a registered user with the leaderboard ID `{databaseUser.LeaderboardId}`."); return; }

				Database.InsertOne(databaseUser);
				await ReplyAsync($"✅ `{user.Username} is now registered.");
			}
			catch
			{
				await ReplyAsync("❌ Couldn't execute command.");
			}
		}
	}
}