using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Database")]
	[Group("register")]
	[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public class AddUser : AbstractModule<SocketCommandContext>
	{
		private readonly DatabaseHelper _databaseHelper;

		public AddUser(DatabaseHelper databaseHelper)
		{
			_databaseHelper = databaseHelper;
		}

		[Command]
		[Priority(1)]
		public async Task Register([Name("Leaderboard ID")] uint lbId, [Remainder] string name)
		{
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u =>
				u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				u.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true);

			if (await IsError(!userMatches.Any(), "User not found.") || await IsError(userMatches.Count() > 1, $"Multiple people in the server have {name.ToLower()} in their name. Mention the user or specify their ID."))
				return;

			await ReplyAsync(await DatabaseHelper.RegisterUser(lbId, Context.Guild.GetUser(userMatches.First().Id)));
		}
	}
}
