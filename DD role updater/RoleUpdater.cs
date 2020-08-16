using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Net.Http;
using System.Text;
using System.IO;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;

namespace Clubber.DdRoleUpdater
{
    [Name("Role Management")]
    public class RoleUpdater : ModuleBase<SocketCommandContext>
    {
        public static Dictionary<int, ulong> ScoreRoleDict = new Dictionary<int, ulong>();
        private static readonly HttpClient Client = new HttpClient();
        private readonly UTF8Encoding UTF8 = new UTF8Encoding(true, true);
        private readonly string DbJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/DdPlayerDataBase.json");
        private readonly string ScoreRoleJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/ScoreRoles.json");

        public RoleUpdater()
        {
            ScoreRoleDict = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(File.ReadAllText(ScoreRoleJsonPath));

            if (!File.Exists(DbJsonPath) || new FileInfo(DbJsonPath).Length == 0)
            {
                Dictionary<ulong, DdUser> emptyDb = new Dictionary<ulong, DdUser>();
                string emptyDbJson = JsonConvert.SerializeObject(emptyDb, Formatting.Indented);
                File.Create(DbJsonPath).Close();
                File.WriteAllText(DbJsonPath, emptyDbJson);
            }
        }

        [Command("updaterolesanddatabase"), Alias("updatedb")]
        [Summary("Updates users' score/club roles that are in the database.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task UpdateRolesAndDataBase()
        {
            var Db = RoleUpdaterHelper.DeserializeDb();
            List<bool> listOfRoleUpdateChecks = Db.Values.Select(async user => await UpdateUserRoles(user)).Select(t => t.Result).ToList();
            if (!listOfRoleUpdateChecks.Contains(true)) { await ReplyAsync("No role updates were needed."); return; }

            await SerializeDbAndReply(Db, await ReplyAsync("Processing..."));
        }

        [Command("adduserbyrank"), Alias("addr")]
        [Summary("Obtains user from their rank and adds them to the database.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task AddUserByRank(uint rank, ulong discordId)
        {
            try
            {
                if (!RoleUpdaterHelper.IsValidDiscordId(discordId, Context.Guild.Users))
                { await ReplyAsync("Invalid ID."); return; }
                if (RoleUpdaterHelper.DiscordIdExistsInDb(discordId))
                { await ReplyAsync($"There already exists a user in the database with the Discord ID `{discordId}`."); return; }

                string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-rank?rank={rank}");
                DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
                DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

                if (RoleUpdaterHelper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId))
                { await ReplyAsync($"There already exists a user in the database with rank `{rank}` and leaderboard ID `{databaseUser.LeaderboardId}`."); return; }

                var Db = RoleUpdaterHelper.DeserializeDb();
                Db.Add(discordId, databaseUser);
                var msg = await ReplyAsync($"✅ User `{GetGuildUser(discordId).Username}` successfully added to database.");
                await SerializeDbAndReply(Db, msg);
            }
            catch
            { await ReplyAsync("❌ Something went wrong. Couldn't execute command."); }
        }

        [Command("adduserbyid"), Alias("addid")]
        [Summary("Obtains user from their leaderboard ID and adds them to the database.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task AddUserByID(uint lbId, ulong discordId)
        {
            try
            {
                if (!RoleUpdaterHelper.IsValidDiscordId(discordId, Context.Guild.Users))
                { await ReplyAsync("Invalid ID."); return; }
                if (RoleUpdaterHelper.DiscordIdExistsInDb(discordId))
                { await ReplyAsync($"There already exists a user in the database with the Discord ID `{discordId}`."); return; }

                string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
                DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
                DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

                if (RoleUpdaterHelper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId))
                { await ReplyAsync($"There already exists a user in the database with the leaderboard ID `{databaseUser.LeaderboardId}`."); return; }

                var Db = RoleUpdaterHelper.DeserializeDb();
                Db.Add(discordId, databaseUser);
                var msg = await ReplyAsync($"✅ User `{GetGuildUser(discordId).Username}` successfully added to database.");
                await SerializeDbAndReply(Db, msg);
            }
            catch
            { await ReplyAsync("❌ Something went wrong. Couldn't execute command."); }
        }

        [Command("remlbid")]
        [Summary("Remove user from database based on their leaderboard ID.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveUserByLeaderboardID(int lbId)
        {
            if (!RoleUpdaterHelper.LeaderboardIdExistsInDb(lbId))
            { await ReplyAsync($"User {lbId} doesn't exist in the database."); return; }

            var Db = RoleUpdaterHelper.DeserializeDb();
            foreach (KeyValuePair<ulong, DdUser> user in Db)
            {
                if (lbId == user.Value.LeaderboardId)
                {
                    var msg = await ReplyAsync($"✅ User `{GetGuildUser(user.Value.DiscordId).Username}` (LB-ID: {lbId}) successfully removed from database.");
                    Db.Remove(user.Key);
                    await SerializeDbAndReply(Db, msg);
                    return;
                }
            }
        }

        [Command("remid")]
        [Summary("Remove user from database based on their Discord ID.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveUserByDiscordId(ulong discordId)
        {
            if (!RoleUpdaterHelper.DiscordIdExistsInDb(discordId))
            { await ReplyAsync($"User {discordId} doesn't exist in the database."); return; }

            var Db = RoleUpdaterHelper.DeserializeDb();
            foreach (KeyValuePair<ulong, DdUser> user in Db)
            {
                if (discordId == user.Value.DiscordId)
                {
                    var msg = await ReplyAsync($"✅ User `{GetGuildUser(discordId).Username}` (LB-ID: {user.Value.LeaderboardId}) successfully removed from database.");
                    Db.Remove(user.Key);
                    await SerializeDbAndReply(Db, msg);
                    return;
                }
            }
        }

        [Command("cleardb")]
        [Summary("Clear the entire database.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task ClearDatabase()
        {
            if (RoleUpdaterHelper.DeserializeDb().Count == 0)
            { await ReplyAsync("The database is already empty."); return; }

            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "⚠️ Are you sure you want to clear the database?",
                Description = "Think twice about this."
            };
            Emoji checkmarkEmote = new Emoji("✅"), xEmote = new Emoji("❌");

            var msg = await ReplyAsync(null, false, embed.Build());
            await msg.AddReactionsAsync(new[] { checkmarkEmote, xEmote });
            Context.Client.ReactionAdded += OnMessageReactedAsync;
        }

        public async Task OnMessageReactedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel originChannel, SocketReaction reaction)
        {
            var msg = cachedMessage.GetOrDownloadAsync();
            if (msg != null && reaction.UserId == Context.User.Id && reaction.Emote.Name == "✅")
            {
                try
                {
                    Context.Client.ReactionAdded -= OnMessageReactedAsync;
                    var Db = RoleUpdaterHelper.DeserializeDb();
                    Db.Clear();
                    var dbClearMsg = await ReplyAsync("✅ Database successfully cleared.");
                    await SerializeDbAndReply(Db, dbClearMsg);
                }
                catch
                {
                    Context.Client.ReactionAdded -= OnMessageReactedAsync;
                    await ReplyAsync("Failed to execute command.");
                }
            }
            else if ((msg != null && reaction.UserId == Context.User.Id && reaction.Emote.Name == "❌") || msg == null)
            {
                Context.Client.ReactionAdded -= OnMessageReactedAsync;
                await ReplyAsync("Cancelled database clearing.");
            }
        }

        [Command("printdb")]
        [Summary("Print the list of users in the database.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task PrintDatabase()
        {
            var Db = RoleUpdaterHelper.DeserializeDb();
            if (Db.Count == 0) { await ReplyAsync("The database is empty."); return; }

            char[] blacklistedCharacters = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/CharacterBlacklist.txt")).ToCharArray();
            StringBuilder desc = new StringBuilder().AppendLine($"`{"User",-16 - 2}{"Discord ID",-18 - 3}{"LB ID",-7 - 3}{"Score",-5 - 3}{"Role",-10}`");
            foreach (DdUser user in Db.Values)
            {
                string userName = GetGuildUser(user.DiscordId).Username;
                var userNameChecked = blacklistedCharacters.Intersect(userName.ToCharArray()).Any() ? "[Too long]" : userName.Length > 14 ? $"{userName.Substring(0, 14)}.." : userName;
                desc.AppendLine($"`{userNameChecked,-16 - 2}{user.DiscordId,-18 - 3}{user.LeaderboardId,-7 - 3}{user.Score + "s",-5 - 3}{GetMemberScoreRoleName(user.DiscordId),-10}`");
            }
            EmbedBuilder embed = new EmbedBuilder().WithTitle("DD player database").WithDescription(desc.ToString());

            await ReplyAsync(null, false, embed.Build());
        }

        [Command("showunregisteredusers"), Alias("showunreg")]
        [Summary("Prints a list of guild members that aren't registered in the database.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task ShowUnregisteredUsers()
        {
            try
            {
                EmbedBuilder embed = new EmbedBuilder { Title = "Unregistered guild members (limited < 2048 char)" };
                var unregisteredMembers = Context.Guild.Users.Where(user => !RoleUpdaterHelper.DiscordIdExistsInDb(user.Id)).Select(u => u.Mention);
                embed.Description = string.Join(' ', unregisteredMembers).Substring(0, 1950); // Good enough number below 2048
                await ReplyAsync(null, false, embed.Build());
            }
            catch { await ReplyAsync("❌ Something went wrong. Couldn't execute command."); return; }
        }

        [Priority(1)]
        [Command("updateroles")]
        [Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
        public async Task UpdateRoles()
        {
            if (RoleUpdaterHelper.DeserializeDb().ContainsKey(Context.User.Id))
            {
                if (!await UpdateUserRoles(RoleUpdaterHelper.GetDdUserFromId(Context.User.Id)))
                    await ReplyAsync($"No updates were needed for you, {Context.User.Username}.");
            }
            else await ReplyAsync($"You're not in my database, {Context.User.Username}. I can therefore not update your roles, so please ask an admin/moderator/role assigner to register you.");
        }

        [Priority(3)]
        [Command("updateroles"), Remarks("├ ")]
        [Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
        public async Task UpdateRoles(IUser userMention)
        {
            if (RoleUpdaterHelper.DeserializeDb().ContainsKey(userMention.Id))
            {
                if (!await UpdateUserRoles(RoleUpdaterHelper.GetDdUserFromId(userMention.Id)))
                    await ReplyAsync($"No updates were needed for {Context.User.Username}.");
            }
            else await ReplyAsync($"User `{userMention.Username}` is not in my database. I can therefore not update their roles, so please ask an admin/moderator/role assigner to register them.");
        }

        [Priority(2)]
        [Command("updateroles"), Remarks("└ ")]
        [Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
        public async Task UpdateRoles([Remainder] string userNameOrNickname)
        {
            string usernameornickname = userNameOrNickname.ToLower();
            IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u => u.Username.ToLower() == usernameornickname || (u.Nickname != null && u.Nickname.ToLower() == usernameornickname));
            switch (userMatches.Count())
            {
                case 0: await ReplyAsync($"Found no user(s) with the username/nickname `{userNameOrNickname}`"); break;
                case 1:
                    if (RoleUpdaterHelper.DeserializeDb().ContainsKey(userMatches.First().Id))
                    {
                        if (!await UpdateUserRoles(RoleUpdaterHelper.GetDdUserFromId(userMatches.First().Id)))
                            await ReplyAsync($"No updates were needed for {Context.User.Username}.");
                    }
                    else await ReplyAsync($"User `{userMatches.First().Username}` is not in my database. I can therefore not update their roles, so please ask an admin/moderator/role assigner to register them.");
                    break;

                default: await ReplyAsync($"Multiple people have the name {userNameOrNickname}. Please specify a username or mention the user instead."); break;
            }
        }

        public async Task<bool> UpdateUserRoles(DdUser user)
        {
            var guildUser = GetGuildUser(user.DiscordId);
            var scoreRole = ScoreRoleDict.Where(sr => sr.Key <= user.Score).OrderByDescending(sr => sr.Key).FirstOrDefault();
            var roleToAdd = Context.Guild.GetRole(scoreRole.Value);
            var removedRoles = RemoveScoreRolesExcept(guildUser, roleToAdd);

            if (RoleUpdaterHelper.MemberHasRole(guildUser, roleToAdd.Id) && removedRoles.Count == 0)
                return false;

            StringBuilder description = new StringBuilder($"{guildUser.Mention}");

            if (removedRoles.Count != 0) description.Append($"\n\nRemoved:\n- {string.Join("\n- ", removedRoles.Select(sr => sr.Mention))}");
            if (!RoleUpdaterHelper.MemberHasRole(guildUser, scoreRole.Value))
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

        public bool SerializeDb(Dictionary<ulong, DdUser> Db)
        {
            try
            {
                var sortedDb = Db.OrderByDescending(db => db.Value.Score).ToDictionary(x => x.Key, x => x.Value);
                string json = JsonConvert.SerializeObject(sortedDb, Formatting.Indented);
                File.WriteAllText(DbJsonPath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SerializeDbAndReply(Dictionary<ulong, DdUser> Db, IUserMessage msg)
        {
            await msg.ModifyAsync(mssg => mssg.Content = $"{msg.Content}\nAttempting database serialization...");

            if (SerializeDb(Db)) await msg.ModifyAsync(mssg => mssg.Content = $"{msg.Content[0..^36]}✅ Database successfully serialized.");
            else await msg.ModifyAsync(mssg => mssg.Content = $"{msg.Content[0..^36]}❌ Failed to serialize database.");
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
