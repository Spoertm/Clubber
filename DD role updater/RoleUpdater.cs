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

namespace Clubber.DDroleupdater
{
    [Name("Role Management")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class RoleUpdater : ModuleBase<SocketCommandContext>
    {
        private static Dictionary<ulong, DdUser> DdPlayerDatabase = new Dictionary<ulong, DdUser>();
        private static Dictionary<int, ulong> ScoreRoleDict = new Dictionary<int, ulong>();
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
                    var memberHasRole = MemberHasRole(member, item.Value);
                    if (user.Value.Score > item.Key && memberHasRole)
                        break;

                    if (user.Value.Score > item.Key && !memberHasRole)
                    {
                        updatedRoles = true;
                        numberOfRoleUpdates++;

                        List<SocketRole> removedRolesList = RemoveScoreRoles(member);
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
            if (UserExists(discordId))
            {
                await ReplyAsync($"User {discordId} already exists in the DB.");
                return;
            }

            IUserMessage msg = await ReplyAsync("Processing...");

            try
            {
                string getUserByRankUrl = await Client.GetStringAsync($"https://devildaggers.info/Api/GetUserByRank?rank={rank}");
                DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(getUserByRankUrl);
                DdUser databaseUser = new DdUser(discordId, lbPlayer.Id)
                {
                    Score = lbPlayer.Time / 10000
                };
                DdPlayerDatabase.Add(discordId, databaseUser);

                await msg.ModifyAsync(msg => msg.Content = "✅ User successfully added to database.");
                await SerializeDbMessage(msg);
            }
            catch
            {
                await msg.ModifyAsync(msg => msg.Content = "❌ Something went wrong. Couldn't execute command.");
            }
        }

        [Command("adduserbyid"), Alias("addid")]
        [Summary("Obtains user from their leaderboard ID and adds them to the database.")]
        public async Task AddUserByID(uint lbID, ulong discordId)
        {
            if (UserExists(discordId))
            {
                await ReplyAsync($"User {discordId} already exists in the DB.");
                return;
            }

            IUserMessage msg = await ReplyAsync("Processing...");

            try
            {
                string getUserByIdUrl = await Client.GetStringAsync($"https://devildaggers.info/Api/GetUserById?userid={lbID}");
                DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(getUserByIdUrl);
                DdUser databaseUser = new DdUser(discordId, lbPlayer.Id)
                {
                    Score = lbPlayer.Time / 10000
                };
                DdPlayerDatabase.Add(discordId, databaseUser);

                await msg.ModifyAsync(msg => msg.Content = "✅ User successfully added to database.");
                await SerializeDbMessage(msg);
            }
            catch
            {
                await msg.ModifyAsync(msg => msg.Content = "❌ Something went wrong. Couldn't execute command.");
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
                    await msg.ModifyAsync(msg => msg.Content = $"✅ User {lbID} successfully removed from database.");
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
            if (!UserExists(discordId))
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
                    await msg.ModifyAsync(msg => msg.Content = $"✅ User {discordId} successfully removed from database.");
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
                    await reply.ModifyAsync(msg => msg.Content = "✅ Database successfully cleared.");
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
                builder.AppendLine($"`{user.DiscordId,-18}\t{user.LeaderboardId,-7}\t{user.Score,-5}`");

            EmbedBuilder embed = new EmbedBuilder { Title = "DD player database" };
            var dbMemberNameMentions = DdPlayerDatabase.Values.Select(user => GetGuildUser(user.DiscordId).Mention);
            var dbMemberRoleMentions = DdPlayerDatabase.Values.Select(user => GetMemberScoreRole(user.DiscordId).Mention);
            embed.AddField(fld =>
            {
                fld.Name = "`User`";
                fld.Value = $"{string.Join('\n', dbMemberNameMentions)}";
                fld.IsInline = true;
            })
            .AddField(fld =>
            {
                fld.Name = $"`{"DiscordId",-18}\t{"LB-ID",-7}\t{"Score",-5}`";
                fld.Value = builder.Append("").ToString();
                fld.IsInline = true;
            })
            .AddField(fld =>
            {
                fld.Name = "`Role`";
                fld.Value = $"{string.Join('\n', dbMemberRoleMentions)}";
                fld.IsInline = true;
            });
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
            await msg.ModifyAsync(msg => msg.Content = $"{msg.Content}\nAttempting database serialization...");

            if (SerializeDB()) await msg.ModifyAsync(mssg => mssg.Content = $"{msg.Content[0..^36]}✅ Database successfully serialized.");
            else await msg.ModifyAsync(mssg => mssg.Content = $"{msg.Content[0..^36]}❌ Failed to serialize database.");
        }

        public bool UserExists(ulong discordId)
        {
            return DdPlayerDatabase.ContainsKey(discordId);
        }

        public SocketGuildUser GetGuildUser(ulong ID)
        {
            return Context.Guild.GetUser(ID);
        }

        public bool MemberHasRole(SocketGuildUser member, ulong roleID)
        {
            foreach (SocketRole role in member.Roles)
            {
                if (role.Id == roleID)
                    return true;
            }
            return false;
        }

        public List<SocketRole> RemoveScoreRoles(SocketGuildUser member)
        {
            List<SocketRole> removedRoles = new List<SocketRole>();

            foreach (var role in member.Roles)
            {
                if (ScoreRoleDict.ContainsValue(role.Id))
                {
                    member.RemoveRoleAsync(role);
                    removedRoles.Add(role);
                }
            }
            return removedRoles;
        }

        public SocketRole GetMemberScoreRole(ulong memberId)
        {
            foreach (SocketRole userRole in GetGuildUser(memberId).Roles)
            {
                foreach (var roleId in ScoreRoleDict.Values)
                {
                    if (userRole.Id == roleId) return userRole;
                }
            }
            return null;
        }
    }

    public class DdPlayer
    {
        public int Rank { get; set; }
        public int Id { get; set; }
        public string Username { get; set; }
        public int Time { get; set; }
        public int Kills { get; set; }
        public int Gems { get; set; }
        public int DeathType { get; set; }
        public int ShotsHit { get; set; }
        public int ShotsFired { get; set; }
        public long TimeTotal { get; set; }
        public int KillsTotal { get; set; }
        public int GemsTotal { get; set; }
        public int DeathsTotal { get; set; }
        public int ShotsHitTotal { get; set; }
        public int ShotsFiredTotal { get; set; }
    }
}
