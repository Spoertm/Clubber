using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Clubber.Databases;
using MongoDB.Bson;
using System.Text;
using Discord.Addons.Interactive;
using Clubber.Files;
using System.Globalization;
using System.Dynamic;

namespace Clubber.Modules
{
	[Name("Database")]
	public class PrintModule : InteractiveBase
	{
		private readonly char[] blacklistedCharacters = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Files/CharacterBlacklist.txt")).ToCharArray();
		private readonly IMongoCollection<DdUser> Database;
		private readonly Dictionary<int, ulong> ScoreRoleDictionary;

		public PrintModule(MongoDatabase mongoDatabase, ScoreRoles scoreRoles)
		{
			Database = mongoDatabase.DdUserCollection;
			ScoreRoleDictionary = scoreRoles.ScoreRoleDictionary;
		}

		[Command("printdb")]
		[Summary("Shows a paginated list of users in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task PrintDatabase()
		{
			int databaseCount = (int)Database.CountDocuments(new BsonDocument());
			int maxpage = (int)Math.Ceiling(databaseCount / 20d);
			StringBuilder embedText = new StringBuilder();
			string[] descriptionArray = new string[maxpage];
			Emote trashcan = Emote.Parse("<:trashcan:765705377857667107>");
			PaginatedMessage paginate = new PaginatedMessage() { Title = $"DD player database\nTotal: {databaseCount}" };
			paginate.Options = new PaginatedAppearanceOptions()
			{
				Stop = trashcan,
				InformationText = $">>> \n◀️ ▶️ - Cycle between pages.\n\n⏮ ⏭️ - Jump to the first or last page.\n\n🔢 - Once pressed it will listen to the user's next message which should be a page number.\n\n{trashcan} - Stops the pagination session and deletes the pagination message.\n\u2800",
				Timeout = TimeSpan.FromMinutes(20),
				FooterFormat = "Page {0}/{1} - " + $"{Context.User.Username}'s session",
			};

			IEnumerable<DdUser> sortedDb = Database.AsQueryable().OrderByDescending(x => x.Score);
			for (int pageNum = 1; pageNum <= maxpage; pageNum++)
			{
				int start = 20 * (pageNum - 1);
				int i = start;

				embedText.Clear().AppendLine($"`{"#",-4}{"User",-17 - 3}{"Discord ID",-18 - 3}{"LB ID",-6 - 3}{"Score",-5 - 3}{"Role",-10}`");

				IEnumerable<DdUser> table = sortedDb.Skip(start).Take(20);
				foreach (DdUser user in table)
				{
					string username = GetCheckedMemberName(user.DiscordId);
					embedText.AppendLine($"`{++i,-4}{username}{user.DiscordId,-18 - 3}{user.LeaderboardId,-6 - 3}{user.Score + "s",-5 - 3}{GetMemberScoreRoleName(user.DiscordId),-10}`");
				}
				descriptionArray[pageNum - 1] = embedText.ToString();
			}
			paginate.Pages = descriptionArray;

			await PagedReplyAsync(paginate);
		}

		public string GetCheckedMemberName(ulong discordId)
		{
			StringBuilder nameBuilder = new StringBuilder();
			var user = Context.Guild.GetUser(discordId);
			if (user == null) return nameBuilder.Append("Not in server").Append(' ', 7).ToString();

			string username = user.Username;
			if (blacklistedCharacters.Intersect(username.ToCharArray()).Any()) return nameBuilder.Append($"{username[0]}..").Append(' ', 17).ToString();

			int nameCount = new StringInfo(username).LengthInTextElements;
			if (nameCount > 15) return nameBuilder.Append($"{GraphemeSubstring(username, 15)}..   ").ToString();
			else if (nameCount < username.Length) return nameBuilder.Append(username).Append(' ', 19 - nameCount).ToString();
			else return nameBuilder.Append(username).Append(' ', 20 - nameCount).ToString();
		}

		public string GraphemeSubstring(string str, int length)
		{
			string substr = "";
			TextElementEnumerator charEnum = StringInfo.GetTextElementEnumerator(str);

			for (int i = 0; i < length; i++)
			{
				charEnum.MoveNext();
				substr += charEnum.GetTextElement();
			}

			return substr;
		}

		public string GetMemberScoreRoleName(ulong memberId)
		{
			var guildUser = Context.Guild.GetUser(memberId);
			if (guildUser == null) return "N.I.S";

			foreach (var userRole in guildUser.Roles)
			{
				foreach (ulong roleId in ScoreRoleDictionary.Values)
				{
					if (userRole.Id == roleId) return userRole.Name;
				}
			}
			return "No role";
		}
	}
}