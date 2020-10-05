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
using Discord.WebSocket;
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
			PaginatedMessage paginate = new PaginatedMessage() { Title = $"DD player database\nTotal: {databaseCount}" };
			paginate.Options = new PaginatedAppearanceOptions()
			{
				Stop = Emote.Parse("<:trashcanUI:762399152385556510>"),
				InformationText = ">>> ◀️ ▶️ - Cycle between pages.\n\n⏮ ⏭️ - Jump to the first or last page.\n\n🔢 - Once pressed it will listen to the user's next message which should be a page number.\n\n<:trashcanUI:762399152385556510> - Stops the pagination session and deletes the pagination message.",
				Timeout = TimeSpan.FromMinutes(20),
				FooterFormat = "Page {0}/{1} - " + $"{Context.User.Username}'s session",
			};

			for (int pageNum = 1; pageNum <= maxpage; pageNum++)
			{
				int start = 20 * (pageNum - 1);
				int i = start;

				embedText.Clear().AppendLine($"`{"#",-4}{"User",-16 - 2}{"Discord ID",-18 - 3}{"LB ID",-7 - 3}{"Score",-5 - 3}{"Role",-10}`");

				IEnumerable<DdUser> sortedDb = Database.AsQueryable().OrderByDescending(x => x.Score).Skip(start).Take(20);
				foreach (DdUser user in sortedDb)
				{
					string username = GetCheckedMemberName(user.DiscordId, blacklistedCharacters);
					embedText.AppendLine($"`{++i,-4}{username,-16 - 2}{user.DiscordId,-18 - 3}{user.LeaderboardId,-7 - 3}{user.Score + "s",-5 - 3}{GetMemberScoreRoleName(user.DiscordId),-10}`");
				}
				descriptionArray[pageNum - 1] = embedText.ToString();
			}
			paginate.Pages = descriptionArray;

			await PagedReplyAsync(paginate);
		}

		[Command("showunregisteredusers"), Alias("showunreg")]
		[Summary("Shows a paginated list of guild members that aren't registered in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task ShowUnregisteredUsers()
		{
			try
			{
				List<string> unregMentions = new List<string>();
				foreach (SocketGuildUser user in Context.Guild.Users)
					AddUserMentionIfUnregistered(user, unregMentions);
				
				int unregisteredCount = unregMentions.Count;
				if (unregisteredCount == 0) { await ReplyAsync("No results to show."); return; }
				int maxFields = (int)Math.Ceiling(unregisteredCount / 15d); // 15 names per field

				PaginatedMessage paginate = new PaginatedMessage() { Title = $"Unregistered guild members\nTotal: {unregisteredCount}" };
				paginate.Options = new PaginatedAppearanceOptions()
				{
					Stop = Emote.Parse("<:trashcanUI:762399152385556510>"),
					InformationText = ">>> ◀️ ▶️ - Cycle between pages.\n\n⏮ ⏭️ - Jump to the first or last page.\n\n🔢 - Once pressed it will listen to the user's next message which should be a page number.\n\n<:trashcanUI:762399152385556510> - Stops the pagination session and deletes the pagination message.",
					Timeout = TimeSpan.FromMinutes(20),
					FooterFormat = "Page {0}/{1} - " + $"{Context.User.Username}'s session",
					FieldsPerPage = 2
				};

				StringBuilder sb = new StringBuilder();
				EmbedFieldBuilder[] fields = new EmbedFieldBuilder[maxFields];

				for (int i = 1; i <= maxFields; i++)
				{
					int start = 15 * (i - 1);
					sb.Clear().Append("・" + string.Join("\n・", unregMentions.Skip(start).Take(15)));
					string test = sb.ToString();
					fields[i - 1] = new EmbedFieldBuilder()
					{
						Name = i.ToString(),
						Value = sb.ToString(),
						IsInline = true
					};
				}
				paginate.Pages = fields;

				await PagedReplyAsync(paginate);
			}
			catch
			{
				await ReplyAsync("❌ Something went wrong. Couldn't execute command.");
				return;
			}
		}

		public void AddUserMentionIfUnregistered(SocketGuildUser user, List<string> mentionList)
		{
			ulong cheaterRoleId = 693432614727581727;
			if (!user.IsBot && !user.Roles.Any(r => r.Id == cheaterRoleId) && !Helper.DiscordIdExistsInDb(user.Id, Database))
			{
				mentionList.Add(user.Mention);
			}
		}

		public string GetCheckedMemberName(ulong discordId, char[] blacklistedCharacters)
		{
			var user = Context.Guild.GetUser(discordId);
			if (user == null) return "Not in server";

			string username = user.Username;
			if (blacklistedCharacters.Intersect(username.ToCharArray()).Any()) return $"{username[0]}..";
			else if (username.Length > 14) return $"{username[0..14]}..";
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