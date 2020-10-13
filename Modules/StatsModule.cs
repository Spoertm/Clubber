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
	[Name("Info")]
	[Group("stats")]
	[Summary("Provides stats on you if you're registered, otherwise says they're unregistered.\nIf a user is specified, then it'll provide their stats instead.")]
	public class StatsModule : ModuleBase<SocketCommandContext>
	{
		private readonly IMongoCollection<DdUser> Database;

		public StatsModule(MongoDatabase mongoDatabase)
		{
			Database = mongoDatabase.DdUserCollection;
		}

		[Command]
		[Priority(1)]
		public async Task Stats()
		{
			ulong cheaterRoleId = 693432614727581727;
			var user = Context.User as SocketGuildUser;
			if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username}, you can't register because you've cheated."); return; }
			if (!Helper.DiscordIdExistsInDb(Context.User.Id, Database)) { await ReplyAsync($"You're not registered in the database, {user.Username}. Please ask an admin/moderator/role assigner to register you."); return; }

			await StatsFromId(user.Id);
		}

		[Command]
		[Priority(3)]
		public async Task StatsFromName([Remainder] string name)
		{
			IEnumerable<IUser> guildMatches = Context.Guild.Users.Where(
				user => user.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				(user.Nickname != null && user.Nickname.Contains(name, StringComparison.InvariantCultureIgnoreCase)));

			int guildMatchesCount = guildMatches.Count();

			if (guildMatchesCount == 0) await ReplyAsync($"Found no user with the name `{name.ToLower()}`.");
			else if (guildMatchesCount == 1) await StatsFromId(guildMatches.First().Id);
			else await ReplyAsync($"Multiple people in the server have `{name.ToLower()}` in their name. Mention the user or specify their ID.");
		}

		[Command]
		[Priority(2)]
		public async Task StatsFromMention(IUser userMention) => await StatsFromId(userMention.Id);

		[Command("id")]
		[Priority(4)]
		public async Task StatsFromId(ulong discordId)
		{
			bool userInGuild = UserIsInGuild(discordId);
			bool userInDb = Helper.DiscordIdExistsInDb(discordId, Database);

			if (!userInGuild && !userInDb) { await ReplyAsync("User not found."); return; }
			if (!userInGuild && userInDb) { await ReplyAsync("User is registered but isn't in the server."); return; }

			ulong cheaterRoleId = 693432614727581727;
			var guildUser = Context.Guild.GetUser(discordId);
			if (guildUser.IsBot) { await ReplyAsync($"{guildUser.Mention} is a bot. It can't be registered as a DD player."); return; }
			if (guildUser.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{guildUser.Username} can't be registered because they've cheated."); return; }
			if (!userInDb) { await ReplyAsync($"`{guildUser.Username}` is not registered. Please ask an admin/moderator/role-assigner to register them."); return; }

			try
			{
				HttpClient httpClient = new HttpClient();

				int userLbId = Helper.GetDdUserFromId(discordId, Database).LeaderboardId;
				string jsonUser = await httpClient.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={userLbId}");
				DdPlayer ddPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);

				EmbedBuilder embed = new EmbedBuilder
				{
					Title = $"{guildUser.Username} is registered",
					Description = $"Leaderboard name: {ddPlayer.Username}\nLeaderboard ID: {ddPlayer.Id}\nScore: {ddPlayer.Time / 10000f}s",
					ThumbnailUrl = guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl()
				};

				await ReplyAsync(null, false, embed.Build());
			}
			catch
			{
				await ReplyAsync("❌ Something went wrong. Couldn't execute command.");
			}
		}

		public bool UserIsInGuild(ulong id)
		{
			return Context.Guild.Users.Any(user => user.Id == id);
		}
	}
}