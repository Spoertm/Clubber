using System.Collections.Generic;
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

		[Priority(1)]
		public async Task Stats()
		{
			ulong cheaterRoleId = 693432614727581727;
			var user = Context.User as SocketGuildUser;
			if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username}, you can't register because you've cheated."); return; }
			else if (!Helper.DiscordIdExistsInDb(Context.User.Id, Database)) { await ReplyAsync($"You're not registered in the database, {user.Username}. Please ask an admin/moderator/role assigner to register you."); return; }

			await StatsFromId(user.Id);
		}

		[Priority(2)]
		public async Task Stats(string name)
		{
			string usernameornickname = name.ToLower();

			IQueryable<DdUser> dbMatches = Database.AsQueryable().Where(
				ddUser => UserIsInGuild(ddUser.DiscordId) &&
				Context.Guild.GetUser(ddUser.DiscordId).Username.ToLower().Contains(usernameornickname));

			IEnumerable<IUser> guildMatches = Context.Guild.Users.Where(
				user => user.Username.ToLower().Contains(usernameornickname) ||
				(user.Nickname != null && user.Nickname.ToLower().Contains(usernameornickname)));

			int dbMatchesCount = dbMatches.Count();
			int guildMatchesCount = guildMatches.Count();

			if (dbMatchesCount == 0 && guildMatchesCount == 0) { await ReplyAsync($"Found no users with the name `{usernameornickname}`."); return; }

			if (dbMatchesCount == 1) { await StatsFromId(dbMatches.First().DiscordId); return; }
			if (dbMatchesCount > 1) { await ReplyAsync($"Multiple people in the database have `{name.ToLower()}` in their name. Mention the user or specify their ID."); return; }

			if (guildMatchesCount == 1) { await StatsFromId(guildMatches.First().Id); return; }
			if (guildMatchesCount > 1) { await ReplyAsync($"Multiple people in the server have `{name.ToLower()}` in their name. Mention the user or specify their ID."); return; }
		}

		[Priority(3)]
		public async Task Stats(IUser userMention) => await StatsFromId(userMention.Id);

		[Priority(4)]
		[Command("id")]
		public async Task StatsFromId(ulong discordId)
		{
			try
			{
				HttpClient httpClient = new HttpClient();
				bool userInGuild = UserIsInGuild(discordId);
				bool userInDb = Helper.DiscordIdExistsInDb(discordId, Database);
				if (userInGuild)
				{
					ulong cheaterRoleId = 693432614727581727;
					var guildUser = Context.Guild.GetUser(discordId);
					if (guildUser.IsBot) { await ReplyAsync($"{guildUser.Mention} is a bot. It can't be registered as a DD player."); return; }
					if (Context.Guild.GetUser(discordId).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{guildUser.Username} can't be registered because they've cheated."); return; }
					if (!userInDb) { await ReplyAsync($"`{Context.Guild.GetUser(discordId).Username}` is not registered in the database. Please ask an admin/moderator/role assigner to register them."); return; }
				}
				if (!userInGuild && !userInDb) { await ReplyAsync($"Failed to find user with the name or ID `{discordId}`."); return; }

				DdUser ddUser = Helper.GetDdUserFromId(discordId, Database);
				string jsonUser = await httpClient.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={ddUser.LeaderboardId}");
				DdPlayer ddPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);


				var test = typeof(DdUser).GetFields();
				var guildMember = Context.Guild.GetUser(discordId);
				EmbedBuilder embed = new EmbedBuilder
				{
					Title = $"{(guildMember == null ? "User" : guildMember.Username)} is registered",
					Description = $"Leaderboard name: {ddPlayer.Username}\nLeaderboard ID: {ddPlayer.Id}\nScore: {ddPlayer.Time / 10000f}s",
					ThumbnailUrl = guildMember.GetAvatarUrl() ?? guildMember.GetDefaultAvatarUrl()
				};
				embed.Description += userInGuild ? null : $"\n\n<@{discordId}> is not a member in {Context.Guild.Name}.";

				await ReplyAsync(null, false, embed.Build());
			}
			catch { await ReplyAsync("❌ Something went wrong. Couldn't execute command."); }
		}

		public bool UserIsInGuild(ulong id)
		{
			return Context.Guild.Users.Any(user => user.Id == id);
		}
	}
}
