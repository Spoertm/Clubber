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
	[Name("Info")]
	[Group("stats")]
	[Summary("Provides stats on you if you're registered, otherwise says they're unregistered.\nIf a user is specified, then it'll provide their stats instead.")]
	public class StatsModule : AbstractModule<SocketCommandContext>
	{
		private readonly DatabaseHelper _databaseHelper;

		public StatsModule(DatabaseHelper databaseHelper)
		{
			_databaseHelper = databaseHelper;
		}

		[Command]
		[Priority(1)]
		public async Task Stats()
		{
			SocketGuildUser? user = Context.User as SocketGuildUser;
			if (await IsError(user.Roles.Any(r => r.Id == Constants.CheaterRoleId), $"{user.Username}, you can't register because you've cheated."))
				return;

			if (await IsError(!_databaseHelper.DiscordIdExistsInDb(Context.User.Id), $"You're not registered in the database, {user.Username}. Please ask an admin/moderator/role assigner to register you."))
				return;

			await StatsFromId(user.Id);
		}

		[Command]
		[Priority(2)]
		public async Task StatsFromName([Remainder] string name)
		{
			IEnumerable<IUser> guildMatches = Context.Guild.Users.Where(
				user => user.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				user.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true);

			int guildMatchesCount = guildMatches.Count();

			if (guildMatchesCount == 0)
				await ReplyAsync("User not found.");
			else if (guildMatchesCount == 1)
				await StatsFromId(guildMatches.First().Id);
			else
				await ReplyAsync($"Multiple people in the server have `{name.ToLower()}` in their name. Mention the user or specify their ID.");
		}

		[Command]
		[Priority(3)]
		public async Task StatsFromMention(IUser userMention) => await StatsFromId(userMention.Id);

		[Command("id")]
		[Priority(4)]
		public async Task StatsFromId(ulong discordId)
		{
			bool userInGuild = Context.Guild.GetUser(discordId) != null;
			bool userInDb = _databaseHelper.DiscordIdExistsInDb(discordId);

			if (await IsError(!userInGuild && !userInDb, "User not found."))
				return;

			if (await IsError(!userInGuild && userInDb, "User is registered but isn't in the server."))
				return;

			SocketGuildUser guildUser = Context.Guild.GetUser(discordId);
			if (await IsError(guildUser.IsBot, $"{guildUser.Mention} is a bot. It can't be registered as a DD player."))
				return;

			if (await IsError(guildUser.Roles.Any(r => r.Id == Constants.CheaterRoleId), $"{guildUser.Username} can't be registered because they've cheated."))
				return;

			if (await IsError(!userInDb, $"`{guildUser.Username}` is not registered. Please ask an admin/moderator/role-assigner to register them."))
				return;

			try
			{
				using HttpClient httpClient = new();

				int userLbId = _databaseHelper.GetDdUserFromId(discordId).LeaderboardId;
				string jsonUser = await httpClient.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={userLbId}");
				DdPlayer ddPlayer = JsonConvert.DeserializeObject<DdPlayer>(jsonUser);

				EmbedBuilder embed = new()
				{
					Title = $"{guildUser.Username} is registered",
					Description = $"Leaderboard name: {ddPlayer.Username}\nLeaderboard ID: {ddPlayer.Id}\nScore: {ddPlayer.Time / 10000f}s",
					ThumbnailUrl = guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl(),
				};

				await ReplyAsync(null, false, embed.Build());
			}
			catch
			{
				await ReplyAsync("❌ Something went wrong. Couldn't execute command.");
			}
		}
	}
}
