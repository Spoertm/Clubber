using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clubber.Files;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using Clubber.Databases;

namespace Clubber.Modules
{
	[Name("Database")]
	public class ShowUnregistered : InteractiveBase
	{
		private readonly IMongoCollection<DdUser> Database;

		public ShowUnregistered(MongoDatabase mongodatabase)
		{
			Database = mongodatabase.DdUserCollection;
		}

		[Command("showunregisteredusers"), Alias("showunreg")]
		[Summary("Shows a paginated list of guild members that aren't registered in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task ShowUnregisteredUsers()
		{
			try
			{
				List<string> unregUsernames = new List<string>();
				ulong cheaterRoleId = 693432614727581727;
				foreach (SocketGuildUser user in Context.Guild.Users)
				{
					if (!user.IsBot && !user.Roles.Any(r => r.Id == cheaterRoleId) && !Helper.DiscordIdExistsInDb(user.Id, Database))
					{
						unregUsernames.Add(user.Username);
					}
				}

				int unregisteredCount = unregUsernames.Count;
				if (unregisteredCount == 0) { await ReplyAsync("No results to show."); return; }
				int maxFields = (int)Math.Ceiling(unregisteredCount / 15d); // 15 names per field

				PaginatedMessage paginate = new PaginatedMessage() { Title = $"Unregistered guild members\nTotal: {unregisteredCount}" };
				Emote trashcan = Emote.Parse("<:trashcan:765705377857667107>");
				paginate.Options = new PaginatedAppearanceOptions()
				{
					Stop = trashcan,
					InformationText = $">>> ◀️ ▶️ - Cycle between pages.\n\n⏮ ⏭️ - Jump to the first or last page.\n\n🔢 - Once pressed it will listen to the user's next message which should be a page number.\n\n{trashcan} - Stops the pagination session and deletes the pagination message.",
					Timeout = TimeSpan.FromMinutes(20),
					FooterFormat = "Page {0}/{1} - " + $"{Context.User.Username}'s session",
					FieldsPerPage = 2
				};

				StringBuilder sb = new StringBuilder();
				EmbedFieldBuilder[] fields = new EmbedFieldBuilder[maxFields];

				for (int i = 1; i <= maxFields; i++)
				{
					int start = 15 * (i - 1);
					sb.Clear().Append("・" + string.Join("\n・", unregUsernames.Skip(start).Take(15)));
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
	}
}
