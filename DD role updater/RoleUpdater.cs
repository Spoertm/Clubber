using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Diagnostics;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Clubber.DdRoleUpdater
{
	public class RoleUpdater : ModuleBase<SocketCommandContext>
	{
		private readonly string ScoreRoleJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/ScoreRoles.json");
		public static Dictionary<int, ulong> ScoreRoleDict = new Dictionary<int, ulong>();
		private static readonly HttpClient Client = new HttpClient();
		private IMongoCollection<DdUser> Database;

		public RoleUpdater()
		{
			try
			{
				ScoreRoleDict = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(File.ReadAllText(ScoreRoleJsonPath));
				MongoClient client = new MongoClient("mongodb+srv://Ali_Alradwy:cEdM5Br52RYlbHaX@cluster0.ffrfn.mongodb.net/Clubber?retryWrites=true&w=majority");
				IMongoDatabase db = client.GetDatabase("Clubber");

				Database = db.GetCollection<DdUser>("DdUsers");
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Failed to initialize RoleUpdater.\n\n{exception.Message}");
			}
		}

		[Command("updaterolesanddatabase"), Alias("updatedb")]
		[Summary("Updates users' score/club roles that are in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task UpdateRolesAndDataBase()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			IUserMessage msg = await ReplyAsync("Processing...");
			IEnumerable<string> nonMemberMentions = Database.AsQueryable().Where(x => GetGuildMember(x.DiscordId) == null).Select(ddUser => $"<@{ddUser.DiscordId}>");

			if (nonMemberMentions.Any())
				await ReplyAsync(null, false, new EmbedBuilder { Title = "Unable to update these users. They're most likely not in the server.", Description = string.Join(' ', nonMemberMentions) }.Build());

			List<Task<bool>> tasks = new List<Task<bool>>();
			foreach (DdUser user in Database.AsQueryable())
				tasks.Add(UpdateUserRoles(user));

			int usersUpdated = (await Task.WhenAll(tasks)).Count(b => b);
			if (usersUpdated > 0)
				await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and member roles for {usersUpdated} users.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
			else
				await msg.ModifyAsync(m => m.Content = $"No role updates were needed.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
		}

		[Priority(3)]
		[Command("addfromrank"), Alias("addr")]
		[Summary("Obtains user from their rank and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByRankAndName(uint rank, [Remainder] string name)
		{
			string lowerCaseName = name.ToLower();
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(lowerCaseName) || (u.Nickname != null && u.Nickname.ToLower().Contains(lowerCaseName)));
			await Helper.AddToDbFromName(userMatches, name, rank, async (rank, discordId) => await AddUserByRankAndId(rank, discordId), Context.Channel);
		}

		[Priority(2)]
		[Command("addfromrank"), Alias("addr"), Remarks("├ ")]
		[Summary("Obtains user from their rank and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByRankAndMention(uint rank, IUser userMention) => await AddUserByRankAndId(rank, userMention.Id);

		[Priority(1)]
		[Command("addfromrankandid"), Alias("addrid")]
		[Summary("Obtains user from their rank and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByRankAndId(uint rank, ulong discordId)
		{
			try
			{
				var user = GetGuildMember(discordId);
				if (Helper.DiscordIdExistsInDb(discordId, Database)) { await ReplyAsync($"User `{(user == null ? "" : user.Username)}({discordId})` is already registered."); return; }
				if (user == null) { await ReplyAsync($"❗ Could not find a user with the name or ID `{discordId}`."); return; }
				ulong cheaterRoleId = 693432614727581727;
				if (user.IsBot) { await ReplyAsync($"{user.Mention} is a bot. It can't be registered as a DD player."); return; }
				if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username} can't be registered because they've cheated."); return; }

				string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-rank?rank={rank}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (Helper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId, Database)) { await ReplyAsync($"There already exists a user in the database with rank `{rank}` and leaderboard ID `{databaseUser.LeaderboardId}`."); return; }

				Database.InsertOne(databaseUser);
				await ReplyAsync($"✅ Added `{user.Username}` to the database.");
			}
			catch
			{ await ReplyAsync("❌ Couldn't execute command."); }
		}

		[Command("addfromlbid"), Alias("addlbid")]
		[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByID(uint lbId, [Remainder] string name)
		{
			string usernameornickname = name.ToLower();
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(usernameornickname) || (u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));
			await Helper.AddToDbFromName(userMatches, name, lbId, async (lbId, discordId) => await AddUserByLbIdAndDscId(lbId, discordId), Context.Channel);
		}

		[Command("addfromlbid"), Alias("addlbid"), Remarks("├ ")]
		[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByID(uint lbId, IUser userMention) => await AddUserByLbIdAndDscId(lbId, userMention.Id);

		[Command("addfromlbidandid"), Alias("addlbandid")]
		[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByLbIdAndDscId(uint lbId, ulong discordId)
		{
			try
			{
				var user = GetGuildMember(discordId);
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

		[Command("remove")]
		[Summary("Remove user from database based on the input info.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task RemoveUser(string name)
		{
			string usernameornickname = name.ToLower();
			var dbMatches = Database.AsQueryable().Where(doc => GetGuildMember(doc.DiscordId) != null && GetGuildMember(doc.DiscordId).Username.ToLower().Contains(usernameornickname)).Select(ddUser => GetGuildMember(ddUser.DiscordId));
			int dbMatchesCount = dbMatches.Count();

			if (dbMatchesCount == 0) await ReplyAsync($"Found no users with the name `{usernameornickname}` in the database.");
			else if (dbMatchesCount == 1) await RemoveUser(dbMatches.First().Id);
			else await ReplyAsync($"Multiple people in the database have `{name.ToLower()}` in their name. Mention the user or specify their ID.");
		}

		[Command("remove"), Remarks("└ ")]
		[Summary("Remove user from database based on the input info.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task RemoveUser(ulong discordId)
		{
			if (!Helper.DiscordIdExistsInDb(discordId, Database)) { await ReplyAsync($"Coudn't find a user with the username or ID `{discordId}` in the database."); return; }

			Database.DeleteOne(x => x.DiscordId == discordId);
			await ReplyAsync($"✅ Removed {(GetGuildMember(discordId) == null ? "User" : $"`{GetGuildMember(discordId).Username}`")} `ID: {discordId}`.");
		}

		[Priority(1)]
		[Command("stats")]
		[Summary("Provides stats on you if you're registered, otherwise says they're unregistered.\nIf a user is specified, then it'll provide their stats instead.")]
		public async Task Stats()
		{
			ulong cheaterRoleId = 693432614727581727;
			var user = Context.User as SocketGuildUser;
			if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username}, you can't register because you've cheated."); return; }
			else if (!Helper.DiscordIdExistsInDb(Context.User.Id, Database)) { await ReplyAsync($"You're not registered in the database, {user.Username}. Please ask an admin/moderator/role assigner to register you. "); return; }

			await StatsFromId(user.Id);
		}

		[Priority(2)]
		[Command("stats"), Remarks("├ ")]
		[Summary("Provides stats on you if you're registered, otherwise says they're unregistered.\nIf a user is specified, then it'll provide their stats instead.")]
		public async Task Stats(string name)
		{
			string usernameornickname = name.ToLower();

			var dbMatches = Database.AsQueryable().Where(ddUser => UserIsInGuild(ddUser.DiscordId) && GetGuildMember(ddUser.DiscordId).Username.ToLower().Contains(usernameornickname)).Select(ddUser => GetGuildMember(ddUser.DiscordId));
			IEnumerable<IUser> guildMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(usernameornickname) || (u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));
			int dbMatchesCount = dbMatches.Count();
			int guildMatchesCount = guildMatches.Count();

			if (dbMatchesCount == 0 && guildMatchesCount == 0) { await ReplyAsync($"Found no users with the name `{usernameornickname}`."); return; }

			if (dbMatchesCount == 1) { await StatsFromId(dbMatches.First().Id); return; }
			if (dbMatchesCount > 1) { await ReplyAsync($"Multiple people in the database have `{name.ToLower()}` in their name. Mention the user or specify their ID."); return; }

			if (guildMatchesCount == 1) { await StatsFromId(guildMatches.First().Id); return; }
			if (guildMatchesCount > 1) { await ReplyAsync($"Multiple people in the server have `{name.ToLower()}` in their name. Mention the user or specify their ID."); return; }
		}

		[Priority(3)]
		[Command("stats"), Remarks("└ ")]
		[Summary("Provides stats on you if you're registered, otherwise says you're unregistered.\nIf a user is specified, then it'll provide their stats instead.")]
		public async Task Stats(IUser userMention) => await StatsFromId(userMention.Id);

		[Priority(4)]
		[Command("statsid")]
		[Summary("Provides stats on a user from their Discord ID.")]
		public async Task StatsFromId(ulong discordId)
		{
			try
			{
				bool userInGuild = UserIsInGuild(discordId);
				bool userInDb = Helper.DiscordIdExistsInDb(discordId, Database);
				if (userInGuild)
				{
					ulong cheaterRoleId = 693432614727581727;
					var guildUser = Context.Guild.GetUser(discordId);
					if (guildUser.IsBot) { await ReplyAsync($"{guildUser.Mention} is a bot. It can't be registered as a DD player."); return; }
					if (GetGuildMember(discordId).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{guildUser.Username} can't be registered because they've cheated."); return; }
					if (!userInDb) { await ReplyAsync($"`{GetGuildMember(discordId).Username}` is not registered in the database. Please ask an admin/moderator/role assigner to register them."); return; }
				}
				if (!userInGuild && !userInDb) { await ReplyAsync($"Failed to find user with the name or ID `{discordId}`."); return; }

				DdUser ddUser = Helper.GetDdUserFromId(discordId, Database);
				string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={ddUser.LeaderboardId}");
				DdPlayer ddPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);


				var test = typeof(DdUser).GetFields();
				var guildMember = GetGuildMember(discordId);
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

		[Priority(1)]
		[Command("updateroles")]
		[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
		public async Task UpdateRoles()
		{
			ulong cheaterRoleId = 693432614727581727;
			var user = Context.User as SocketGuildUser;
			if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username}, you can't register because you've cheated."); return; }
			else if (Helper.DiscordIdExistsInDb(user.Id, Database))
			{
				if (!await UpdateUserRoles(Helper.GetDdUserFromId(user.Id, Database)))
					await ReplyAsync($"No updates were needed for you, {user.Username}.");
			}
			else await ReplyAsync($"You're not in my database, {user.Username}. I can therefore not update your roles, so please ask an admin/moderator/role assigner to register you.");
		}

		[Priority(3)]
		[Command("updateroles"), Remarks("└ ")]
		[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
		public async Task UpdateRoles([Remainder] string name)
		{
			string usernameornickname = name.ToLower();

			var dbMatches = Database.AsQueryable().ToList().Where(ddUser => UserIsInGuild(ddUser.DiscordId) && GetGuildMember(ddUser.DiscordId).Username.ToLower().Contains(usernameornickname));
			IEnumerable<IUser> guildMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(usernameornickname) || (u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));
			int dbMatchesCount = dbMatches.Count();
			int guildMatchesCount = guildMatches.Count();

			if (dbMatchesCount == 0 && guildMatchesCount == 0) { await ReplyAsync($"Found no users with the name `{usernameornickname}`."); return; }

			if (dbMatchesCount == 1) { await UpdateRolesFromId(dbMatches.First().DiscordId); return; }
			if (dbMatchesCount > 1) { await ReplyAsync($"Multiple people in the database have `{name.ToLower()}` in their name. Mention the user or specify their ID."); return; }

			if (guildMatchesCount == 1) { await UpdateRolesFromId(guildMatches.First().Id); return; }
			if (guildMatchesCount > 1) { await ReplyAsync($"Multiple people in the server have `{name.ToLower()}` in their name. Mention the user or specify their ID."); return; }
		}

		[Priority(2)]
		[Command("updateroles"), Remarks("├ ")]
		[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
		public async Task UpdateRoles(IUser userMention) => await UpdateRolesFromId(userMention.Id);

		[Command("updaterolesID")]
		[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
		public async Task UpdateRolesFromId(ulong discordId)
		{
			bool userIsInGuild = UserIsInGuild(discordId);
			bool userInDb = Helper.DiscordIdExistsInDb(discordId, Database);
			if (!userIsInGuild && !userInDb) { await ReplyAsync($"Failed to find user with the name or ID `{discordId}`."); return; }
			if (userIsInGuild)
			{
				ulong cheaterRoleId = 693432614727581727;
				var guildUser = Context.Guild.GetUser(discordId);
				if (guildUser.IsBot) { await ReplyAsync($"{guildUser.Mention} is a bot. It can't be registered as a DD player."); return; }
				if (Context.Guild.GetUser(discordId).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{guildUser.Username} can't be registered because they've cheated."); return; }
				if (!userInDb) { await ReplyAsync($"`{GetGuildMember(discordId).Username}` is not registered in the database. Please ask an admin/moderator/role assigner to register them."); return; }
				if (userInDb)
				{
					if (!await UpdateUserRoles(Helper.GetDdUserFromId(discordId, Database)))
						await ReplyAsync($"No updates were needed for {guildUser.Username}.");
				}
				else await ReplyAsync($"{guildUser.Username} is not in my database. I can therefore not update their roles, so please ask an admin/moderator/role assigner to register them.");
			}
			else await ReplyAsync("That user is in the database but not in the server.");
		}

		public async Task<bool> UpdateUserRoles(DdUser user)
		{
			SocketGuildUser guildMember = Context.Guild.GetUser(user.DiscordId);
			if (guildMember == null || !UserIsInGuild(guildMember.Id))
				return false; // User not in server

			user.Score = await GetUserTimeFromHasmodai(user.LeaderboardId) / 10000;
			Database.FindOneAndUpdate(x => x.DiscordId == user.DiscordId, Builders<DdUser>.Update.Set(x => x.Score, user.Score));

			KeyValuePair<int, ulong> scoreRole = ScoreRoleDict.Where(sr => sr.Key <= user.Score).OrderByDescending(sr => sr.Key).FirstOrDefault();
			SocketRole roleToAdd = Context.Guild.GetRole(scoreRole.Value);
			List<SocketRole> removedRoles = await RemoveScoreRolesExcept(guildMember, roleToAdd);

			if (removedRoles.Count == 0 && Helper.MemberHasRole(guildMember, roleToAdd.Id))
				return false;

			EmbedBuilder embed = new EmbedBuilder
			{
				Title = $"Updated roles for {guildMember.Username}",
				Description = $"User: {guildMember.Mention}",
				ThumbnailUrl = guildMember.GetAvatarUrl() ?? guildMember.GetDefaultAvatarUrl()
			};

			if (removedRoles.Count > 0)
				embed.AddField(new EmbedFieldBuilder()
				{
					Name = "Removed:",
					Value = string.Join('\n', removedRoles.Select(sr => sr.Mention)),
					IsInline = true
				});

			if (!Helper.MemberHasRole(guildMember, scoreRole.Value))
			{
				if (roleToAdd != null)
				{
					await guildMember.AddRoleAsync(roleToAdd);
					embed.AddField(new EmbedFieldBuilder()
					{
						Name = "Added:",
						Value = roleToAdd.Mention,
						IsInline = true
					});
				}
				else embed.Description += $"\nFailed to find role from role ID, but it should have been the one for {scoreRole.Key}s+.";
			}

			await ReplyAsync(null, false, embed.Build());
			return true;
		}

		private async Task<int> GetUserTimeFromHasmodai(int userId)
		{
			Dictionary<string, string> postValues = new Dictionary<string, string> { { "uid", userId.ToString() } };

			using FormUrlEncodedContent content = new FormUrlEncodedContent(postValues);
			using HttpClient client = new HttpClient();
			HttpResponseMessage resp = await client.PostAsync("http://dd.hasmodai.com/backend16/get_user_by_id_public.php", content);
			byte[] data = await resp.Content.ReadAsByteArrayAsync();

			int bytePos = 19;
			short usernameLength = BitConverter.ToInt16(data, bytePos);
			bytePos += usernameLength + sizeof(short);
			return BitConverter.ToInt32(data, bytePos + 12);
		}

		public string GetMemberScoreRoleName(ulong memberId)
		{
			if (!UserIsInGuild(memberId)) return "N.I.S";
			var guildUser = Context.Guild.GetUser(memberId);
			foreach (var userRole in guildUser.Roles)
			{
				foreach (ulong roleId in ScoreRoleDict.Values)
				{
					if (userRole.Id == roleId) return userRole.Name;
				}
			}
			return "No role";
		}

		public SocketGuildUser GetGuildMember(ulong id)
		{
			return Context.Guild.GetUser(id);
		}

		public bool UserIsInGuild(ulong id)
		{
			return Context.Guild.Users.Any(user => user.Id == id);
		}

		public string GetCheckedMemberName(ulong discordId, char[] blacklistedCharacters)
		{
			var user = GetGuildMember(discordId);
			if (user == null) return "Not in server";

			string username = user.Username;
			if (blacklistedCharacters.Intersect(username.ToCharArray()).Any()) return $"{username[0]}..";
			else if (username.Length > 14) return $"{username.Substring(0, 14)}..";
			else return username;
		}

		public async Task<List<SocketRole>> RemoveScoreRolesExcept(SocketGuildUser member, SocketRole excludedRole)
		{
			List<SocketRole> removedRoles = new List<SocketRole>();

			foreach (var role in member.Roles)
			{
				if (ScoreRoleDict.ContainsValue(role.Id) && role.Id != excludedRole.Id)
				{
					await member.RemoveRoleAsync(role);
					removedRoles.Add(role);
				}
			}
			return removedRoles;
		}
	}
}