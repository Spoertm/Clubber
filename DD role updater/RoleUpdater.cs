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
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public class RoleUpdater : ModuleBase<SocketCommandContext>
    {
        public Dictionary<ulong, DdUser> DdPlayerDatabase = new Dictionary<ulong, DdUser>();
        public Dictionary<int, ulong> ScoreRoleDict = new Dictionary<int, ulong>();
        private static readonly HttpClient Client = new HttpClient();
        private readonly UTF8Encoding UTF8 = new UTF8Encoding(true, true);
        private readonly string DBJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/DdPlayerDataBase.json");
        private readonly string ScoreRoleJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/ScoreRoles.json");

        public RoleUpdater()
        {
            ScoreRoleDict = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(File.ReadAllText(ScoreRoleJsonPath));

            if (File.Exists(DBJsonPath))
            {
                if (new FileInfo(DBJsonPath).Length > 0)
                    DdPlayerDatabase = JsonConvert.DeserializeObject<Dictionary<ulong, DdUser>>(File.ReadAllText(DBJsonPath));
            }
            else File.Create(DBJsonPath);
        }

        [Command("UpdateRolesAndDatabase"), Alias("updatedb")]
        [Summary("Updates users' score/club roles that are in the database.")]
        public async Task UpdateRolesAndDataBase()
        {
            List<bool> listOfRoleUpdateChecks = DdPlayerDatabase.Values.Select(async user => await UpdateUserRoles(user)).Select(t => t.Result).ToList();

            if (!listOfRoleUpdateChecks.Contains(true)) await ReplyAsync("No role updates were needed.");
        }
        
        [Command("AddUserByRank"), Alias("addr")]
        [Summary("Obtains user from their rank and adds them to the database.")]
        public async Task AddUserByRank(uint rank, ulong discordId)
        {
            if (RoleUpdaterHelper.UserExistsInDb(discordId))
            { await ReplyAsync($"User {discordId} already exists in the DB."); return; }

            try
            {
                string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-rank?rank={rank}");
                DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
                DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

                DdPlayerDatabase.Add(discordId, databaseUser);
                var msg = await ReplyAsync("✅ User successfully added to database.");
                await SerializeDbMessage(msg);
            }
            catch
            {
                await ReplyAsync("❌ Something went wrong. Couldn't execute command.");
            }
        }

        [Command("AddUserById"), Alias("addid")]
        [Summary("Obtains user from their leaderboard ID and adds them to the database.")]
        public async Task AddUserByID(uint lbId, ulong discordId)
        {
            if (RoleUpdaterHelper.UserExistsInDb(discordId))
            { await ReplyAsync($"User {discordId} already exists in the DB."); return; }

            try
            {
                string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
                DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
                DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

                DdPlayerDatabase.Add(discordId, databaseUser);
                var msg = await ReplyAsync("✅ User successfully added to database.");
                await SerializeDbMessage(msg);
            }
            catch
            {
                await ReplyAsync("❌ Something went wrong. Couldn't execute command.");
            }
        }

        [Command("RemLbId")]
        [Summary("Remove user from database based on their leaderboard ID.")]
        public async Task RemoveUserByLeaderboardID(uint lbId)
        {
            foreach (KeyValuePair<ulong, DdUser> user in DdPlayerDatabase)
            {
                if (lbId == user.Value.LeaderboardId)
                {
                    DdPlayerDatabase.Remove(user.Key);
                    var msg = await ReplyAsync($"✅ User {lbId} successfully removed from database.");
                    await SerializeDbMessage(msg);
                    return;
                }
            }
            await ReplyAsync($"User {lbId} doesn't exist in the database.");
        }

        [Command("RemId")]
        [Summary("Remove user from database based on their Discord ID.")]
        public async Task RemoveUserByDiscordId(ulong discordId)
        {
            if (!RoleUpdaterHelper.UserExistsInDb(discordId))
            { await ReplyAsync($"User {discordId} doesn't exist in the database."); return; }

            foreach (KeyValuePair<ulong, DdUser> user in DdPlayerDatabase)
            {
                if (discordId == user.Value.DiscordId)
                {
                    DdPlayerDatabase.Remove(user.Key);
                    var msg = await ReplyAsync($"✅ User {discordId} successfully removed from database.");
                    await SerializeDbMessage(msg);
                    return;
                }
            }
        }

        [Command("ClearDb")]
        [Summary("Clear the entire database.")]
        public async Task ClearDatabase()
        {
            if (DdPlayerDatabase.Count == 0)
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
                    DdPlayerDatabase.Clear();
                    var dbClearMsg = await ReplyAsync("✅ Database successfully cleared.");
                    await SerializeDbMessage(dbClearMsg);
                    Context.Client.ReactionAdded -= OnMessageReactedAsync;
                }
                catch
                {
                    await ReplyAsync("Failed to execute command.");
                    Context.Client.ReactionAdded -= OnMessageReactedAsync;
                }
            }
            else if ((msg != null && reaction.UserId == Context.User.Id && reaction.Emote.Name == "❌") || msg == null)
            {
                await ReplyAsync("Cancelled database clearing.");
                Context.Client.ReactionAdded -= OnMessageReactedAsync;
            }
        }

        [Command("PrintDb")]
        [Summary("Print the list of users in the database.")]
        public async Task PrintDatabase()
        {
            if (DdPlayerDatabase.Count == 0)
            { await ReplyAsync("The database is empty."); return; }

            StringBuilder builder = new StringBuilder();

            foreach (DdUser user in DdPlayerDatabase.Values)
                builder.AppendLine($"`{user.DiscordId,-18}\t{user.LeaderboardId,-7}\t{user.Score + "s",-5}`");

            EmbedBuilder embed = new EmbedBuilder { Title = "DD player database" };

            embed.AddField(RoleUpdaterHelper.BuildField("`User`", $"{string.Join('\n', GetDbNameMentionList())}", true))
                 .AddField(RoleUpdaterHelper.BuildField($"`{"DiscordId",-18}\t{"LB-ID",-7}\t{"Score",-5}`", builder.Append("").ToString(), true))
                 .AddField(RoleUpdaterHelper.BuildField("`Role`", $"{string.Join('\n', GetDbRoleMentionList())}", true));

            await ReplyAsync(null, false, embed.Build());
        }

        public async Task<bool> UpdateUserRoles(DdUser user)
        {
            var guildUser = GetGuildUser(user.DiscordId);
            var scoreRole = ScoreRoleDict.Where(sr => sr.Key <= user.Score).OrderByDescending(sr => sr.Key).FirstOrDefault();
            var roleToAdd = Context.Guild.GetRole(scoreRole.Value);
            var removedRoles = RoleUpdaterHelper.RemoveScoreRolesExcept(guildUser, roleToAdd);
            StringBuilder description = new StringBuilder($"{guildUser.Mention}");

            if (RoleUpdaterHelper.MemberHasRole(guildUser, roleToAdd.Id) && removedRoles.Count == 0)
                return false;

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

        public bool SerializeDB()
        {
            try
            {
                string json = JsonConvert.SerializeObject(DdPlayerDatabase, Formatting.Indented);
                File.WriteAllText(DBJsonPath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SerializeDbMessage(IUserMessage msg)
        {
            await msg.ModifyAsync(mssg => mssg.Content = $"{msg.Content}\nAttempting database serialization...");

            if (SerializeDB()) await msg.ModifyAsync(mssg => mssg.Content = $"{msg.Content[0..^36]}✅ Database successfully serialized.");
            else await msg.ModifyAsync(mssg => mssg.Content = $"{msg.Content[0..^36]}❌ Failed to serialize database.");
        }

        public List<string> GetDbNameMentionList()
        {
            List<string> dbNameMentionList = new List<string>();

            foreach (DdUser user in DdPlayerDatabase.Values)
            {
                dbNameMentionList.Add(GetGuildUser(user.DiscordId).Mention);
            }

            return dbNameMentionList;
        }

        public string GetMemberScoreRoleMention(ulong memberId)
        {
            foreach (SocketRole userRole in GetGuildUser(memberId).Roles)
            {
                foreach (ulong roleId in ScoreRoleDict.Values)
                {
                    if (userRole.Id == roleId) return userRole.Mention;
                }
            }
            return "No role";
        }

        public List<string> GetDbRoleMentionList()
        {
            List<string> dbMemberRoleMentionList = new List<string>();

            foreach (DdUser user in DdPlayerDatabase.Values)
            {
                dbMemberRoleMentionList.Add(GetMemberScoreRoleMention(user.DiscordId));
            }

            return dbMemberRoleMentionList;
        }

        public SocketGuildUser GetGuildUser(ulong Id)
        {
            return Context.Guild.GetUser(Id);
        }
    }
}
