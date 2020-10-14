using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Clubber.Databases;
using Clubber.Files;
using Discord;
using Discord.Commands;
using MongoDB.Driver;

namespace Clubber.Modules
{
	[Name("Database")]
	public class RemoveModule : ModuleBase<SocketCommandContext>
	{
		private readonly IMongoCollection<DdUser> Database;

		public RemoveModule(MongoDatabase mongoDatabase)
		{
			Database = mongoDatabase.DdUserCollection;
		}

		[Command("remove")]
		[Summary("Remove user from database based on the input info.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task RemoveUser(string memberName)
		{
			IEnumerable<DdUser> dbMatches = Database.AsQueryable().ToList().Where(
				user => Context.Guild.GetUser(user.DiscordId) != null &&
				Context.Guild.GetUser(user.DiscordId).Username.Contains(memberName, System.StringComparison.InvariantCultureIgnoreCase));
			int dbMatchesCount = dbMatches.Count();

			if (dbMatchesCount == 0) await ReplyAsync($"Found no users with the name `{memberName.ToLower()}` in the database.");
			else if (dbMatchesCount == 1)
			{
				ulong idToRemove = dbMatches.First().DiscordId;
				Database.DeleteOne(x => x.DiscordId == idToRemove);
				await ReplyAsync($"✅ Removed {(Context.Guild.GetUser(idToRemove) == null ? "User" : $"`{Context.Guild.GetUser(idToRemove).Username}`")} `ID: {idToRemove}`.");
			}
			else await ReplyAsync($"Multiple people in the database have `{memberName.ToLower()}` in their name. Mention the user or specify their ID.");
		}
	}
}