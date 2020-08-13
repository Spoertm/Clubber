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
    [RequireUserPermission(GuildPermission.Administrator)]
    public class RoleUpdater : ModuleBase<SocketCommandContext>
    {
        public static Dictionary<ulong, DdUser> DdPlayerDatabase = new Dictionary<ulong, DdUser>();
        public static Dictionary<int, ulong> ScoreRoleDict = new Dictionary<int, ulong>();
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

        [Command("updateroles")]
        [Summary("Updates users' score/club roles that are in the database.")]
        public async Task UpdateRoles()
        {
            bool updatedRoles = false;
            int numberOfRoleUpdates = 0;
            foreach (var user in DdPlayerDatabase)
            {
                var member = GetGuildUser(user.Value.DiscordId);
                foreach (var item in ScoreRoleDict)
                {
                    var memberHasRole = RoleUpdaterHelper.MemberHasRole(member, item.Value);
                    if (user.Value.Score >= item.Key && memberHasRole)
                        break;

                    if (user.Value.Score >= item.Key && !memberHasRole)
                    {
                        updatedRoles = true;
                        numberOfRoleUpdates++;

                        List<SocketRole> removedRolesList = RoleUpdaterHelper.RemoveScoreRoles(member);
                        SocketRole roleToAdd = Context.Guild.GetRole(item.Value);
                        if (roleToAdd != null) await member.AddRoleAsync(roleToAdd);


                        EmbedBuilder embed = new EmbedBuilder
                        {
                            Title = $"Updated roles for {member.Username}",
                            Description = $"{member.Mention}\n"
                        };

                        if (removedRolesList.Count > 0)
                            embed.Description += $"Removed:\n- {string.Join("\n- ", removedRolesList.Select(role => role.Mention))}";

                        embed.Description += roleToAdd switch
                        {
                            null => $"\nAdded:\nNone. Failed to find role from role ID, but it should have been the one for {item.Key}s+.",
                            _ => $"\nAdded:\n- {roleToAdd.Mention}"
                        };

                        await ReplyAsync(null, false, embed.Build());
                        break;
                    }
                }
            }

            if (!updatedRoles) await ReplyAsync($"No user roles updates required.");
            else await ReplyAsync($"Updated {numberOfRoleUpdates} user(s)' roles.");
        }

        [Command("adduserbyrank"), Alias("addr")]
        [Summary("Obtains user from their rank and adds them to the database.")]
        public async Task AddUserByRank(uint rank, ulong discordId)
        {
            if (RoleUpdaterHelper.UserExists(discordId))
            {
                await ReplyAsync($"User {discordId} already exists in the DB.");
                return;
            }

            IUserMessage msg = await ReplyAsync("Processing...");

            try
            {
                string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-rank?rank={rank}");
                DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
                DdUser databaseUser = new DdUser(discordId, lbPlayer.Id)
                {
                    Score = lbPlayer.Time / 10000
                };
                DdPlayerDatabase.Add(discordId, databaseUser);

                await msg.ModifyAsync(mssg => mssg.Content = "✅ User successfully added to database.");
                await SerializeDbMessage(msg);
            }
            catch
            {
                await msg.ModifyAsync(mssg => mssg.Content = "❌ Something went wrong. Couldn't execute command.");
            }
        }

        [Command("adduserbyid"), Alias("addid")]
        [Summary("Obtains user from their leaderboard ID and adds them to the database.")]
        public async Task AddUserByID(uint lbID, ulong discordId)
        {
            if (RoleUpdaterHelper.UserExists(discordId))
            {
                await ReplyAsync($"User {discordId} already exists in the DB.");
                return;
            }

            IUserMessage msg = await ReplyAsync("Processing...");

            try
            {
                string jsonUser = await Client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbID}");
                DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
                DdUser databaseUser = new DdUser(discordId, lbPlayer.Id)
                {
                    Score = lbPlayer.Time / 10000
                };
                DdPlayerDatabase.Add(discordId, databaseUser);

                await msg.ModifyAsync(mssg => mssg.Content = "✅ User successfully added to database.");
                await SerializeDbMessage(msg);
            }
            catch
            {
                await msg.ModifyAsync(mssg => mssg.Content = "❌ Something went wrong. Couldn't execute command.");
            }
        }

        [Command("removeuserbylbid"), Alias("remlbid")]
        [Summary("Remove user from database based on their leaderboard ID.")]
        public async Task RemoveUserByID(uint lbID)
        {
            IUserMessage msg = await ReplyAsync("Processing...");
            foreach (KeyValuePair<ulong, DdUser> user in DdPlayerDatabase)
            {
                if (lbID == user.Value.LeaderboardId)
                {
                    DdPlayerDatabase.Remove(user.Key);
                    await msg.ModifyAsync(mssg => mssg.Content = $"✅ User {lbID} successfully removed from database.");
                    await SerializeDbMessage(msg);
                    return;
                }
            }

            await ReplyAsync($"User {lbID} doesn't exist in the database.");
        }

        [Command("removeuserdiscid"), Alias("remdid")]
        [Summary("Remove user from database based on their Discord ID.")]
        public async Task RemoveUserByDiscordId(ulong discordId)
        {
            if (!RoleUpdaterHelper.UserExists(discordId))
            {
                await ReplyAsync($"User {discordId} doesn't exist in the database.");
                return;
            }

            IUserMessage msg = await ReplyAsync("Processing...");
            foreach (KeyValuePair<ulong, DdUser> user in DdPlayerDatabase)
            {
                if (discordId == user.Value.DiscordId)
                {
                    DdPlayerDatabase.Remove(user.Key);
                    await msg.ModifyAsync(mssg => mssg.Content = $"✅ User {discordId} successfully removed from database.");
                    await SerializeDbMessage(msg);
                    return;
                }
            }
        }

        [Command("clearDB")]
        [Summary("Clear the entire database.")]
        public async Task ClearDatabase()
        {
            if (DdPlayerDatabase.Count == 0)
            {
                await ReplyAsync("The database is already empty.");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "⚠️ Are you sure you want to clear the database?",
                Description = "Think twice about this."
            };
            Emoji checkmarkEmote = new Emoji("✅");
            Emoji xEmote = new Emoji("❌");

            IUserMessage msg = await ReplyAsync(null, false, embed.Build());
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
                    IUserMessage reply = await ReplyAsync("Processing...");
                    DdPlayerDatabase.Clear();
                    await reply.ModifyAsync(mssg => mssg.Content = "✅ Database successfully cleared.");
                    await SerializeDbMessage(reply);
                    Context.Client.ReactionAdded -= OnMessageReactedAsync;
                }
                catch
                {
                    await ReplyAsync("Failed to execute command.");
                    Context.Client.ReactionAdded -= OnMessageReactedAsync;
                }
            }
            else if (msg != null && reaction.UserId == Context.User.Id && reaction.Emote.Name == "❌")
            {
                await ReplyAsync("Cancelled database clearing.");
                Context.Client.ReactionAdded -= OnMessageReactedAsync;
            }
        }

        [Command("printdb")]
        [Summary("Print the list of users in the database.")]
        public async Task PrintDatabase()
        {
            if (DdPlayerDatabase.Count == 0)
            { await ReplyAsync("The database is empty."); return; }

            StringBuilder builder = new StringBuilder();

            foreach (DdUser user in DdPlayerDatabase.Values)
                builder.AppendLine($"`{user.DiscordId,-18}\t{user.LeaderboardId,-7}\t{user.Score + "s",-5}`");

            EmbedBuilder embed = new EmbedBuilder { Title = "DD player database" };

            embed.AddField(RoleUpdaterHelper.BuildEmbed("`User`", $"{string.Join('\n', GetDbNameMentionList())}", true))
                 .AddField(RoleUpdaterHelper.BuildEmbed($"`{"DiscordId",-18}\t{"LB-ID",-7}\t{"Score",-5}`", builder.Append("").ToString(), true))
                 .AddField(RoleUpdaterHelper.BuildEmbed("`Role`", $"{string.Join('\n', GetDbRoleMentionList())}", true));

            await ReplyAsync(null, false, embed.Build());
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

            foreach (DdUser user in RoleUpdater.DdPlayerDatabase.Values)
            {
                dbNameMentionList.Add(GetGuildUser(user.DiscordId).Mention);
            }

            return dbNameMentionList;
        }

        public string GetMemberScoreRoleMention(ulong memberId)
        {
            foreach (SocketRole userRole in GetGuildUser(memberId).Roles)
            {
                foreach (ulong roleId in RoleUpdater.ScoreRoleDict.Values)
                {
                    if (userRole.Id == roleId) return userRole.Mention;
                }
            }
            return "No role";
        }

        public List<string> GetDbRoleMentionList()
        {
            List<string> dbMemberRoleMentionList = new List<string>();

            foreach (DdUser user in RoleUpdater.DdPlayerDatabase.Values)
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
