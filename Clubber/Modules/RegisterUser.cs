using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Database")]
	[Group("register")]
	[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public class RegisterUser : AbstractModule<SocketCommandContext>
	{
		[Command]
		[Priority(1)]
		public async Task RegisterByName([Name("Leaderboard ID")] uint lbId, [Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneGuildUser(name);
			if (success && user != null)
				await RegisterByTag(lbId, user);
		}

		[Command]
		[Priority(2)]
		public async Task RegisterByTag([Name("Leaderboard ID")] uint lbId, [Name("User tag")] SocketGuildUser user)
		{
			if (!await UserIsClean(user, true, true, true, false))
				return;

			await ReplyAsync(await DatabaseHelper.RegisterUser(lbId, user));
		}
	}
}
