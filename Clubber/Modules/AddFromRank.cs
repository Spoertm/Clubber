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
	[Group("addfromrank"), Alias("addr")]
	[Summary("Obtains user from their rank and adds them to the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public class AddFromRank : AbstractModule<SocketCommandContext>
	{
		private readonly DatabaseHelper _databaseHelper;

		public AddFromRank(DatabaseHelper databaseHelper)
		{
			_databaseHelper = databaseHelper;
		}

		[Command]
		[Priority(1)]
		public async Task AddUserByRankAndName(uint rank, [Remainder] string name)
		{
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u =>
				u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				u.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true);
			await DatabaseHelper.AddToDbFromName(userMatches, rank, async (rank, discordId) => await AddUserByRankAndId(rank, discordId));
		}

		[Command]
		[Priority(2)]
		public async Task AddUserByRankAndMention(uint rank, IUser userMention) => await AddUserByRankAndId(rank, userMention.Id);

		[Command("id")]
		[Priority(3)]
		public async Task AddUserByRankAndId(uint rank, ulong discordId)
		{
			SocketGuildUser user = Context.Guild.GetUser(discordId);
			if (await IsError(_databaseHelper.DiscordIdExistsInDb(discordId), $"User `{(user?.Username ?? string.Empty)}({discordId})` is already registered."))
				return;

			if (await IsError(user == null, "User not found."))
				return;

			if (await IsError(user.IsBot, $"{user.Mention} is a bot. It can't be registered as a DD player."))
				return;

			const ulong cheaterRoleId = 693432614727581727;
			if (await IsError(user.Roles.Any(r => r.Id == cheaterRoleId), $"{user.Username} can't be registered because they've cheated."))
				return;

			try
			{
				using HttpClient client = new();
				string jsonUser = await client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-rank?rank={rank}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (await IsError(_databaseHelper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId), $"There already exists a registered user with rank `{rank}` and leaderboard ID `{databaseUser.LeaderboardId}`."))
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