using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Clubber.DdRoleUpdater;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Clubber.Modules
{
	[Name("Pagination Commands")]
    public class InteractiveCommands : InteractiveBase
    {
		private readonly string ScoreRoleJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/ScoreRoles.json");
		private IMongoCollection<DdUser> Database;
		public static Dictionary<int, ulong> ScoreRoleDict = new Dictionary<int, ulong>();

		public InteractiveCommands()
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
				Console.WriteLine($"Failed to initialize InteractiveCommands.\n\n{exception.Message}");
			}
		}

		[Command("printdb")]
        [Summary("Shows a paginated list of users in the database.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task PrintDatabase()
		{
			int databaseCount = (int)Database.CountDocuments(new BsonDocument());
			int maxpage = (int)Math.Ceiling(databaseCount / 20d);
			StringBuilder embedText = new StringBuilder();
			char[] blacklistedCharacters = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/CharacterBlacklist.txt")).ToCharArray();
			string[] descriptionArray = new string[maxpage];

			for (int pageNum = 1; pageNum <= maxpage; pageNum++)
            {
				int start = 20 * (pageNum - 1);
				int i = start;
				embedText.Clear()
				.AppendLine($"**DD player database\nTotal: {databaseCount}**")
				.AppendLine($"`{"#",-4}{"User",-16 - 2}{"Discord ID",-18 - 3}{"LB ID",-7 - 3}{"Score",-5 - 3}{"Role",-10}`");

				IEnumerable<DdUser> sortedDb = Database.AsQueryable().OrderByDescending(x => x.Score).Skip(start).Take(20);
				foreach (DdUser user in sortedDb)
				{
					string username = GetCheckedMemberName(user.DiscordId, blacklistedCharacters);
					embedText.AppendLine($"`{++i,-4}{username,-16 - 2}{user.DiscordId,-18 - 3}{user.LeaderboardId,-7 - 3}{user.Score + "s",-5 - 3}{GetMemberScoreRoleName(user.DiscordId),-10}`");
				}
				descriptionArray[pageNum - 1] = embedText.ToString();
			}

			await PagedReplyAsync(descriptionArray);
		}

		[Command("showunregisteredusers"), Alias("showunreg")]
		[Summary("Shows a paginated list of guild members that aren't registered in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task ShowUnregisteredUsers()
		{
			try
			{
				ulong cheaterRoleId = 693432614727581727;
				var unregisteredMembersNoCheaters = Context.Guild.Users.Where(user => !user.IsBot && !Helper.DiscordIdExistsInDb(user.Id, Database) && !user.Roles.Any(r => r.Id == cheaterRoleId)).Select(u => u.Mention);
				int unregisteredCount = unregisteredMembersNoCheaters.Count();
				int maxpage = (int)Math.Ceiling(unregisteredCount / 30d);

				StringBuilder sb = new StringBuilder();
				string[] descriptionArray = new string[maxpage];
				for (int pageNum = 1; pageNum <= maxpage; pageNum++)
				{
					int start = 30 * (pageNum - 1);
					sb.Clear()
					.AppendLine($"**Unregistered guild members\nTotal: {unregisteredCount}**")
					.Append(string.Join(' ', unregisteredMembersNoCheaters.Skip(start).Take(30)));
					descriptionArray[pageNum - 1] = sb.ToString();
				}

				await PagedReplyAsync(descriptionArray);
			}
			catch 
			{ 
				await ReplyAsync("❌ Something went wrong. Couldn't execute command."); 
				return; 
			}
		}

		public string GetCheckedMemberName(ulong discordId, char[] blacklistedCharacters)
		{
			var user = Context.Guild.GetUser(discordId);
			if (user == null) return "Not in server";

			string username = user.Username;
			if (blacklistedCharacters.Intersect(username.ToCharArray()).Any()) return $"{username[0]}..";
			else if (username.Length > 14) return $"{username.Substring(0, 14)}..";
			else return username;
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

		public bool UserIsInGuild(ulong id)
		{
			return Context.Guild.Users.Any(user => user.Id == id);
		}
	}
}