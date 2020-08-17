using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Clubber.DdRoleUpdater
{
	[Name("Role Management")]
	public class RoleUpdater : ModuleBase<SocketCommandContext>
	{
		public static Dictionary<int, ulong> ScoreRoleDict = new Dictionary<int, ulong>();
		private static readonly HttpClient Client = new HttpClient();
		private readonly string DbJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/DdPlayerDataBase.json");
		private readonly string ScoreRoleJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/ScoreRoles.json");

		public RoleUpdater()
		{
			ScoreRoleDict = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(File.ReadAllText(ScoreRoleJsonPath));

			if (!File.Exists(DbJsonPath) || new FileInfo(DbJsonPath).Length == 0)
			{
				string emptyDbJson = JsonConvert.SerializeObject(new Dictionary<ulong, DdUser>(), Formatting.Indented);
				File.Create(DbJsonPath).Close();
				File.WriteAllText(DbJsonPath, emptyDbJson);
			}
		}

		[Command("updaterolesanddatabase"), Alias("updatedb")]
		[Summary("Updates users' score/club roles that are in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task UpdateRolesAndDataBase()
		{
			var Db = Helper.DeserializeDb();
			bool updatedDbRolesBool = Db.Values.Select(async user => await UpdateUserRoles(user)).Select(t => t.Result).ToList().Contains(true);
			if (!updatedDbRolesBool) { await ReplyAsync("No role updates were needed."); return; }

			var msg = await ReplyAsync("Processing...");
			await SerializeDbAndReply(Db, "✅ Successfully updated database and member roles.");
			await msg.DeleteAsync();
		}

		[Command("addfromrank"), Alias("addr")]
		[Summary("Obtains user from their rank and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByRank(uint rank, [Remainder] string name)
		{
			string lowerCaseName = name.ToLower();
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(lowerCaseName) || (u.Nickname != null && u.Nickname.ToLower().Contains(lowerCaseName)));
			await Helper.AddToDbFromName(userMatches, name, rank, async (rank, discordId) => await AddUserByRank(rank, discordId), Context.Channel);
		}

		[Command("addfromrank"), Alias("addr"), Remarks("├ ")]
		[Summary("Obtains user from their rank and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByRank(uint rank, IUser userMention) => await AddUserByRank(rank, userMention.Id);

		[Command("addfromrank"), Alias("addr"), Remarks("└ ")]
		[Summary("Obtains user from their rank and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByRank(uint rank, ulong discordId)
		{
			try
			{
				if (!Helper.IsValidDiscordId(discordId, Context.Guild.Users))
				{ await ReplyAsync("Invalid ID."); return; }
				ulong cheaterRoleId = 693432614727581727;
				if (GetGuildUser(GetGuildUser(discordId).Id).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{GetGuildUser(discordId).Username} can't be registered because they've cheated."); return; }
				if (GetGuildUser(discordId).IsBot) { await ReplyAsync($"{GetGuildUser(discordId).Mention} is a bot. It can't be registered as a DD player."); return; }
				if (Helper.DiscordIdExistsInDb(discordId))
				{ await ReplyAsync($"There already exists a user in the database with the Discord ID `{discordId}`."); return; }

				string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-rank?rank={rank}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (Helper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId))
				{ await ReplyAsync($"There already exists a user in the database with rank `{rank}` and leaderboard ID `{databaseUser.LeaderboardId}`."); return; }

				var Db = Helper.DeserializeDb();
				Db.Add(discordId, databaseUser);
				await SerializeDbAndReply(Db, $"✅ Added `{GetGuildUser(discordId).Username}` to the database.");
			}
			catch
			{ await ReplyAsync("❌ Something went wrong. Couldn't execute command."); }
		}

		[Command("addfromlbid"), Alias("addid")]
		[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByID(uint lbId, [Remainder] string name)
		{
			string usernameornickname = name.ToLower();
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(usernameornickname) || (u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));
			await Helper.AddToDbFromName(userMatches, name, lbId, async (lbId, discordId) => await AddUserByID(lbId, discordId), Context.Channel);
		}

		[Command("addfromlbid"), Alias("addid"), Remarks("├ ")]
		[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByID(uint lbId, IUser userMention) => await AddUserByID(lbId, userMention.Id);

		[Command("addfromlbid"), Alias("addid"), Remarks("└ ")]
		[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task AddUserByID(uint lbId, ulong discordId)
		{
			try
			{
				if (!Helper.IsValidDiscordId(discordId, Context.Guild.Users)) { await ReplyAsync("Invalid ID."); return; }
				if (GetGuildUser(discordId).IsBot) { await ReplyAsync($"{GetGuildUser(discordId).Mention} is a bot. It can't be registered as a DD player."); return; }
				if (Helper.DiscordIdExistsInDb(discordId)) { await ReplyAsync($"There already exists a user in the database with the Discord ID `{discordId}`."); return; }

				string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (Helper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId))
				{ await ReplyAsync($"There already exists a user in the database with the leaderboard ID `{databaseUser.LeaderboardId}`."); return; }

				var Db = Helper.DeserializeDb();
				Db.Add(discordId, databaseUser);
				await SerializeDbAndReply(Db, $"✅ Added `{GetGuildUser(discordId).Username}` to the database.");
			}
			catch
			{ await ReplyAsync("❌ Something went wrong. Couldn't execute command."); }
		}

		[Command("remove")]
		[Summary("Remove user from database based on the input info.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task RemoveUser(string name)
		{
			string usernameornickname = name.ToLower();
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(usernameornickname) || (u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));
			int numberOfMatches = userMatches.Count();

			if (numberOfMatches == 0) await ReplyAsync($"Found no user(s) with the username/nickname `{name}`.");
			else if (numberOfMatches == 1) await RemoveUser(userMatches.First().Id);
			else await ReplyAsync($"Multiple people have the name {name.ToLower()}. Please specify a username or mention the user instead.");
		}

		/*[Command("remlbid")]
		[Summary("Remove user from database based on their leaderboard ID.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task RemoveUserByLeaderboardID(int lbId)
		{
			if (!Helper.LeaderboardIdExistsInDb(lbId))
			{ await ReplyAsync($"User {lbId} doesn't exist in the database."); return; }

			var Db = Helper.DeserializeDb();
			foreach (KeyValuePair<ulong, DdUser> user in Db)
			{
				if (lbId == user.Value.LeaderboardId)
				{
					Db.Remove(user.Key);
					await SerializeDbAndReply(Db, $"✅ Removed `{GetGuildUser(user.Key).Username}, LB-ID: {lbId}`.");
					return;
				}
			}
		}
		*/

		[Command("remove")]
		[Summary("Remove user from database based on the input info.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task RemoveUser(ulong discordId)
		{
			if (!Helper.DiscordIdExistsInDb(discordId))
			{ await ReplyAsync($"User {discordId} doesn't exist in the database."); return; }

			var Db = Helper.DeserializeDb();
			foreach (KeyValuePair<ulong, DdUser> user in Db)
			{
				if (discordId == user.Value.DiscordId)
				{
					Db.Remove(user.Key);
					await SerializeDbAndReply(Db, $"✅ Removed `{GetGuildUser(discordId).Username}, ID: {discordId}`.");
					return;
				}
			}
		}

		[Command("cleardb")]
		[Summary("Clear the entire database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task ClearDatabase()
		{
			if (Helper.DeserializeDb().Count == 0)
			{ await ReplyAsync("The database is already empty."); return; }

			Emoji checkmarkEmote = new Emoji("✅"), xEmote = new Emoji("❌");
			EmbedBuilder embed = new EmbedBuilder
			{
				Title = "⚠️ Are you sure you want to clear the database?",
				Description = "Think twice about this."
			};

			var msg = await ReplyAsync(null, false, embed.Build());
			await msg.AddReactionsAsync(new[] { checkmarkEmote, xEmote });
			Context.Client.ReactionAdded += OnMessageReactedAsync;
		}

		public async Task OnMessageReactedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel originChannel, SocketReaction reaction)
		{
			var msg = await cachedMessage.GetOrDownloadAsync();
			if (msg != null && reaction.UserId == Context.User.Id)
			{
				try
				{
					if (reaction.Emote.Name == "✅")
					{
						Context.Client.ReactionAdded -= OnMessageReactedAsync;
						var Db = Helper.DeserializeDb();
						File.Delete(DbJsonPath);
						await ReplyAsync("✅ Cleared database.");
					}
					else if (reaction.Emote.Name == "❌")
					{
						Context.Client.ReactionAdded -= OnMessageReactedAsync;
						await ReplyAsync("Cancelled database clearing.");
					}
				}
				catch
				{
					Context.Client.ReactionAdded -= OnMessageReactedAsync;
					await ReplyAsync("❌ Failed to execute command. Cancelled database clearing.");
				}
			}
			else if (msg == null)
			{
				Context.Client.ReactionAdded -= OnMessageReactedAsync;
				await ReplyAsync("Cancelled database clearing.");
			}
		}

		[Command("printdb")]
		[Summary("Print the list of users in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task PrintDatabase(uint page = 1)
		{
			if (page < 1) { await ReplyAsync("Invalid page number."); return; }
			var Db = Helper.DeserializeDb();
			if (Db.Count == 0) { await ReplyAsync("The database is empty."); return; }
			int maxpage = (int)Math.Ceiling(Db.Count() / 20d);
			if (page > maxpage) { await ReplyAsync($"Page number exceeds the maximum of `{maxpage}`."); return; }

			char[] blacklistedCharacters = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/CharacterBlacklist.txt")).ToCharArray();
			int start = 0 + 20 * ((int)page - 1);
			int end = start + 20 > Db.Count() ? Db.Count() : start + 20;
			StringBuilder desc = new StringBuilder().AppendLine($"`{"#",-4}{"User",-16 - 2}{"Discord ID",-18 - 3}{"LB ID",-7 - 3}{"Score",-5 - 3}{"Role",-10}`");

			int i = start;
			foreach (DdUser user in Db.Values.Skip(start).Take(end))
			{
				string userName = GetGuildUser(user.DiscordId).Username;
				var userNameChecked = blacklistedCharacters.Intersect(userName.ToCharArray()).Any() ? "[Too long]" : userName.Length > 14 ? $"{userName.Substring(0, 14)}.." : userName;
				desc.AppendLine($"`{++i,-4}{userNameChecked,-16 - 2}{user.DiscordId,-18 - 3}{user.LeaderboardId,-7 - 3}{user.Score + "s",-5 - 3}{GetMemberScoreRoleName(user.DiscordId),-10}`");
			}
			EmbedBuilder embed = new EmbedBuilder().WithTitle($"DD player database ({page}/{maxpage})\nTotal: {Db.Count()}").WithDescription(desc.ToString());

			await ReplyAsync(null, false, embed.Build());
		}

		[Command("showunregisteredusers"), Alias("showunreg")]
		[Summary("Prints a list of guild members that aren't registered in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task ShowUnregisteredUsers(uint page = 1)
		{
			try
			{
				if (page < 1) { await ReplyAsync("Invalid page number."); return; }
				var Db = Helper.DeserializeDb();
				if (Db.Count == 0) { await ReplyAsync("The database is empty."); return; }
				int maxpage = (int)Math.Ceiling(Context.Guild.Users.Count() / 50d);
				if (page > maxpage) { await ReplyAsync($"Page number exceeds the maximum of `{maxpage}`."); return; }

				ulong cheaterRoleId = 693432614727581727;
				uint start = 0 + 50 * (page - 1);
				uint end = start + 50;
				var unregisteredMembersNoCheaters = Context.Guild.Users.Where(user => !user.IsBot && !Helper.DiscordIdExistsInDb(user.Id) && !user.Roles.Any(r => r.Id == cheaterRoleId)).Select(u => u.Mention);
				EmbedBuilder embed = new EmbedBuilder { Title = $"Unregistered guild members ({page}/{maxpage})\nTotal: {unregisteredMembersNoCheaters.Count()}" };
				embed.Description = string.Join(' ', unregisteredMembersNoCheaters.Skip((int)start).Take((int)end));
				await ReplyAsync(null, false, embed.Build());
			}
			catch { await ReplyAsync("❌ Something went wrong. Couldn't execute command."); return; }
		}

		[Priority(1)]
		[Command("stats")]
		[Summary("Provides stats on you if you're registered, otherwise says they're unregistered.\nIf a user is specified, then it'll provide their stats instead.")]
		public async Task Stats()
		{
			ulong cheaterRoleId = 693432614727581727;
			if (GetGuildUser(Context.User.Id).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{Context.User.Username}, you can't register because you've cheated."); return; }
			if (!Helper.DiscordIdExistsInDb(Context.User.Id)) { await ReplyAsync($"You're not registered in the database, {Context.User.Username}. Please ask an admin/moderator/role assigner to register you. "); return; }

			await Stats(Context.User);
		}

		[Priority(3)]
		[Command("stats"), Remarks("├ ")]
		[Summary("Provides stats on you if you're registered, otherwise says they're unregistered.\nIf a user is specified, then it'll provide their stats instead.")]
		public async Task Stats(string name)
		{
			string usernameornickname = name.ToLower();
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(usernameornickname) || (u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));
			await Helper.ExecuteFromName(userMatches, name, async (name) => await Stats(userMatches.First()), Context.Channel);
		}

		[Priority(2)]
		[Command("stats"), Remarks("└ ")]
		[Summary("Provides stats on you if you're registered, otherwise says they're unregistered.\nIf a user is specified, then it'll provide their stats instead.")]
		public async Task Stats(IUser userMention)
		{
			try
			{
				if (userMention.IsBot) { await ReplyAsync($"{userMention} is a bot. It can't be registered as a DD player."); return; }
				ulong cheaterRoleId = 693432614727581727;
				if (GetGuildUser(userMention.Id).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{userMention.Username} can't be registered because they've cheated."); return; }
				if (!Helper.DiscordIdExistsInDb(userMention.Id)) { await ReplyAsync($"`{userMention.Username}` is not registered in the database. Please ask an admin/moderator/role assigner to register them."); return; }

				DdUser ddUser = Helper.GetDdUserFromId(userMention.Id);
				string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={ddUser.LeaderboardId}");
				DdPlayer ddPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);

				EmbedBuilder embed = new EmbedBuilder
				{
					Title = $"{userMention.Username} is registered",
					Description = $"Leaderboard name: {ddPlayer.Username}\nScore: {ddPlayer.Time / 10000f}s"
				};

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
			if (GetGuildUser(Context.User.Id).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{Context.User.Username}, you can't register because you've cheated."); return; }
			else if (Helper.DeserializeDb().ContainsKey(Context.User.Id))
			{
				if (!await UpdateUserRoles(Helper.GetDdUserFromId(Context.User.Id)))
					await ReplyAsync($"No updates were needed for you, {Context.User.Username}.");
			}
			else await ReplyAsync($"You're not in my database, {Context.User.Username}. I can therefore not update your roles, so please ask an admin/moderator/role assigner to register you.");
		}

		[Priority(3)]
		[Command("updateroles"), Remarks("├ ")]
		[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
		public async Task UpdateRoles(IUser userMention)
		{
			if (userMention.IsBot) { await ReplyAsync($"{userMention} is a bot. It can't be registered as a DD player."); return; }
			ulong cheaterRoleId = 693432614727581727;
			if (GetGuildUser(userMention.Id).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{userMention.Username} can't be registered because they've cheated."); return; }
			else if (Helper.DeserializeDb().ContainsKey(userMention.Id))
			{
				if (!await UpdateUserRoles(Helper.GetDdUserFromId(userMention.Id)))
					await ReplyAsync($"No updates were needed for {Context.User.Username}.");
			}
			else await ReplyAsync($"User `{userMention.Username}` is not in my database. I can therefore not update their roles, so please ask an admin/moderator/role assigner to register them.");
		}

		[Priority(2)]
		[Command("updateroles"), Remarks("└ ")]
		[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
		public async Task UpdateRoles([Remainder] string name)
		{
			string usernameornickname = name.ToLower();
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(usernameornickname) || (u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));
			await Helper.ExecuteFromName(userMatches, name, async (name) => await UpdateRoles(userMatches.First()), Context.Channel);
		}

		public async Task<bool> UpdateUserRoles(DdUser user)
		{
			string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={user.LeaderboardId}");
			DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
			if (lbPlayer.Time / 10000 > user.Score) user.Score = lbPlayer.Time / 10000;

			var guildUser = GetGuildUser(user.DiscordId);
			var scoreRole = ScoreRoleDict.Where(sr => sr.Key <= user.Score).OrderByDescending(sr => sr.Key).FirstOrDefault();
			var roleToAdd = Context.Guild.GetRole(scoreRole.Value);
			var removedRoles = RemoveScoreRolesExcept(guildUser, roleToAdd);

			if (Helper.MemberHasRole(guildUser, roleToAdd.Id) && removedRoles.Count == 0)
				return false;

			StringBuilder description = new StringBuilder($"{guildUser.Mention}");

			if (removedRoles.Count != 0) description.Append($"\n\nRemoved:\n- {string.Join("\n- ", removedRoles.Select(sr => sr.Mention))}");
			if (!Helper.MemberHasRole(guildUser, scoreRole.Value))
			{
				if (roleToAdd != null)
					await guildUser.AddRoleAsync(roleToAdd);
				description.AppendLine(roleToAdd == null ? $"Failed to find role from role ID, but it should have been the one for {scoreRole.Key}s+." : $"\n\nAdded:\n- {roleToAdd.Mention}");
			}

			EmbedBuilder embed = new EmbedBuilder
			{
				Title = $"Updated roles for {guildUser.Username}",
				Description = description.ToString()
			};
			await ReplyAsync(null, false, embed.Build());
			return true;
		}

		public async Task SerializeDbAndReply(Dictionary<ulong, DdUser> db, string msg)
		{
			try
			{
				var sortedDb = db.OrderByDescending(db => db.Value.Score).ToDictionary(x => x.Key, x => x.Value);
				string json = JsonConvert.SerializeObject(sortedDb, Formatting.Indented);
				File.WriteAllText(DbJsonPath, json);
				await ReplyAsync(msg);
			}
			catch
			{
				await ReplyAsync($"❌ Failed to execute command.\nThis is most likely because the database is already being updated, so please try again shortly.");
			}
		}

		public string GetMemberScoreRoleName(ulong memberId)
		{
			foreach (SocketRole userRole in GetGuildUser(memberId).Roles)
			{
				foreach (ulong roleId in ScoreRoleDict.Values)
				{
					if (userRole.Id == roleId) return userRole.Name;
				}
			}
			return "No role";
		}

		public SocketGuildUser GetGuildUser(ulong Id)
		{
			return Context.Guild.GetUser(Id);
		}

		public List<SocketRole> RemoveScoreRolesExcept(SocketGuildUser member, SocketRole excludedRole)
		{
			List<SocketRole> removedRoles = new List<SocketRole>();

			foreach (var role in member.Roles)
			{
				if (ScoreRoleDict.ContainsValue(role.Id) && role.Id != excludedRole.Id)
				{
					member.RemoveRoleAsync(role);
					removedRoles.Add(role);
				}
			}
			return removedRoles;
		}
	}
}
