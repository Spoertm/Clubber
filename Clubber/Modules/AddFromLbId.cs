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
	[Group("addfromlbid"), Alias("addlbid")]
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
		public async Task AddUserById(uint lbId, [Remainder] string name)
		{
			IEnumerable<SocketGuildUser> userMatches = Context.Guild.Users.Where(u =>
			u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
			(u.Nickname != null && u.Nickname.Contains(name, StringComparison.InvariantCultureIgnoreCase)));
			var response = await _databaseHelper.AddToDbFromName(userMatches, lbId, async (lbId, discordId) => await AddUserByLbIdAndDscId(lbId, discordId));

			if (response.NumberOfMatches == 0)
				await Context.Channel.SendMessageAsync($"No user found.");
			else if (response.NumberOfMatches > 1)
				await Context.Channel.SendMessageAsync($"Multiple people have the name `{name.ToLower()}`. Try mentioning the user.");
		}

		[Command]
		[Priority(2)]
		public async Task AddUserById(uint lbId, IUser userMention)
			=> await AddUserByLbIdAndDscId(lbId, userMention.Id);

		[Command("id")]
		[Priority(3)]
		public async Task AddUserByLbIdAndDscId(uint lbId, ulong discordId)
		{
			SocketGuildUser user = Context.Guild.GetUser(discordId);
			if (await Validate(_databaseHelper.DiscordIdExistsInDb(discordId), $"User `{(user == null ? "" : user.Username)}({discordId})` is already registered.") ||
				await Validate(user == null, "User not found.") ||
				await Validate(user.IsBot, $"{user.Mention} is a bot. It can't be registered as a DD player.") ||
				await Validate(user.Roles.Any(r => r.Id == Constants.CheaterRoleId), $"{user.Username} can't be registered because they've cheated."))
				return;

			try
			{
				HttpClient client = new HttpClient();
				string jsonUser = await client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");
				DdPlayer lbPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
				DdUser databaseUser = new DdUser(discordId, lbPlayer.Id) { Score = lbPlayer.Time / 10000 };

				if (await Validate(_databaseHelper.LeaderboardIdExistsInDb(databaseUser.LeaderboardId), $"There already exists a registered user with the leaderboard ID `{databaseUser.LeaderboardId}`."))
					return;

				_databaseHelper.AddUser(databaseUser);
				await ReplyAsync($"✅ Added `{user.Username}` to the database.");
			}
			catch
			{
				await ReplyAsync("❌ Couldn't execute command.");
			}
		}
	}
}