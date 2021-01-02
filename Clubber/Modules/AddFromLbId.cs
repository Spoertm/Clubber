using Clubber.Files;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Database")]
	[Group("addfromlbid")]
	[Alias("addlbid")]
	[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public class AddFromLbId : AbstractModule<SocketCommandContext>
	{
		private readonly DatabaseHelper _databaseHelper;

		public AddFromLbId(DatabaseHelper databaseHelper)
		{
			_databaseHelper = databaseHelper;
		}

		[Command]
		[Priority(1)]
		public async Task AddUserByID(uint lbId, [Remainder] string name)
		{
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u =>
				u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				u.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true);
			await DatabaseHelper.AddToDbFromName(userMatches, lbId, async (lbId, discordId) => await AddUserByLbIdAndDscId(lbId, discordId));
		}

		[Command]
		[Priority(2)]
		public async Task AddUserByID(uint lbId, IUser userMention) => await AddUserByLbIdAndDscId(lbId, userMention.Id);

		[Command("id")]
		[Priority(3)]
		public async Task AddUserByLbIdAndDscId(uint lbId, ulong discordId)
		{
			SocketGuildUser user = Context.Guild.GetUser(discordId);
			if (await IsError(_databaseHelper.DiscordIdExistsInDb(discordId), $"User `{user?.Username ?? string.Empty}({discordId})` is already registered."))
				return;

			if (await IsError(user == null, "User not found."))
				return;

			if (await IsError(user!.IsBot, $"{user.Mention} is a bot. It can't be registered as a DD player."))
				return;

			const ulong cheaterRoleId = 693432614727581727;
			if (await IsError(user.Roles.Any(r => r.Id == cheaterRoleId), $"{user.Username} can't be registered because they've cheated."))
				return;

			try
			{
				using HttpClient client = new();
				string jsonUser = await client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (await IsError(_databaseHelper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId), $"There already exists a registered user with the leaderboard ID `{databaseUser.LeaderboardId}`."))
					return;

				_databaseHelper.AddUser(databaseUser);
				await ReplyAsync($"✅ `{user.Username}` is now registered.");
			}
			catch
			{
				await ReplyAsync("❌ Couldn't execute command.");
			}
		}
	}
}
