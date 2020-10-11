using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Diagnostics;
using Discord.WebSocket;
using Clubber.Files;
using Clubber.Databases;

namespace Clubber.Modules
{
	[Name("Roles")]
	[Group("updateroles")]
	[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
	public class RoleUpdateModule : ModuleBase<SocketCommandContext>
	{
		private readonly IMongoCollection<DdUser> Database;
		private readonly Dictionary<int, ulong> ScoreRoleDictionary;

		public RoleUpdateModule(MongoDatabase mongoDatabase, ScoreRoles scoreRoles)
		{
			Database = mongoDatabase.DdUserCollection;
			ScoreRoleDictionary = scoreRoles.ScoreRoleDictionary;
		}

		[Command("database")]
		[Summary("Updates users' score/club roles that are in the database.")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task UpdateRolesAndDataBase()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			IUserMessage msg = await ReplyAsync("Processing...");
			IEnumerable<string> nonMemberMentions = Database.AsQueryable()
				.Where(x => Context.Guild.GetUser(x.DiscordId) == null)
				.Select(ddUser => $"<@{ddUser.DiscordId}>");

			if (nonMemberMentions.Any())
				await ReplyAsync(null, false, new EmbedBuilder { Title = "Unable to update these users. They're most likely not in the server.", Description = string.Join(' ', nonMemberMentions) }.Build());

			List<Task<bool>> tasks = new List<Task<bool>>();
			foreach (DdUser user in Database.AsQueryable())
				tasks.Add(UpdateUserRoles(user));

			int usersUpdated = (await Task.WhenAll(tasks)).Count(b => b);
			if (usersUpdated > 0)
				await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and member roles for {usersUpdated} users.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
			else
				await msg.ModifyAsync(m => m.Content = $"No role updates were needed.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
		}

		[Priority(1)]
		[Command]
		public async Task UpdateRoles()
		{
			ulong cheaterRoleId = 693432614727581727;
			var user = Context.User as SocketGuildUser;
			if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username}, you can't register because you've cheated."); return; }
			else if (Helper.DiscordIdExistsInDb(user.Id, Database))
			{
				if (!await UpdateUserRoles(Helper.GetDdUserFromId(user.Id, Database)))
					await ReplyAsync($"No updates were needed for you, {user.Username}.");
			}
			else await ReplyAsync($"You're not in my database, {user.Username}. I can therefore not update your roles, so please ask an admin/moderator/role assigner to register you.");
		}

		[Priority(3)]
		[Command]
		public async Task UpdateRoles([Remainder] string name)
		{
			string usernameornickname = name.ToLower();

			IEnumerable<DdUser> dbMatches = Database.AsQueryable().ToList().Where(
				ddUser => Context.Guild.Users.Any(x => x.Id == ddUser.DiscordId) &&
				Context.Guild.GetUser(ddUser.DiscordId).Username.ToLower().Contains(usernameornickname));

			IEnumerable<IUser> guildMatches = Context.Guild.Users.Where(
				u => u.Username.ToLower().Contains(usernameornickname) ||
				(u.Nickname != null && u.Nickname.ToLower().Contains(usernameornickname)));

			int dbMatchesCount = dbMatches.Count();
			int guildMatchesCount = guildMatches.Count();

			if (dbMatchesCount == 0 && guildMatchesCount == 0) { await ReplyAsync($"Found no users with the name `{usernameornickname}`."); return; }

			if (dbMatchesCount == 1) { await UpdateRolesFromId(dbMatches.First().DiscordId); return; }
			if (dbMatchesCount > 1) { await ReplyAsync($"Multiple people in the database have `{name.ToLower()}` in their name. Mention the user or specify their ID."); return; }

			if (guildMatchesCount == 1) { await UpdateRolesFromId(guildMatches.First().Id); return; }
			if (guildMatchesCount > 1) { await ReplyAsync($"Multiple people in the server have `{name.ToLower()}` in their name. Mention the user or specify their ID."); return; }
		}

		[Priority(2)]
		[Command]
		public async Task UpdateRoles(IUser userMention) => await UpdateRolesFromId(userMention.Id);

		[Command("id")]
		public async Task UpdateRolesFromId(ulong discordId)
		{
			bool userIsInGuild = Context.Guild.Users.Any(x => x.Id == discordId);
			bool userInDb = Helper.DiscordIdExistsInDb(discordId, Database);
			if (!userIsInGuild && !userInDb) { await ReplyAsync($"Failed to find user with the name or ID `{discordId}`."); return; }
			if (userIsInGuild)
			{
				ulong cheaterRoleId = 693432614727581727;
				var guildUser = Context.Guild.GetUser(discordId);
				if (guildUser.IsBot) { await ReplyAsync($"{guildUser.Mention} is a bot. It can't be registered as a DD player."); return; }
				if (Context.Guild.GetUser(discordId).Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{guildUser.Username} can't be registered because they've cheated."); return; }
				if (!userInDb) { await ReplyAsync($"`{Context.Guild.GetUser(discordId).Username}` is not registered in the database. Please ask an admin/moderator/role assigner to register them."); return; }
				if (userInDb)
				{
					if (!await UpdateUserRoles(Helper.GetDdUserFromId(discordId, Database)))
						await ReplyAsync($"No updates were needed for {guildUser.Username}.");
				}
				else await ReplyAsync($"{guildUser.Username} is not in my database. I can therefore not update their roles, so please ask an admin/moderator/role assigner to register them.");
			}
			else await ReplyAsync("That user is in the database but not in the server.");
		}

		public async Task<bool> UpdateUserRoles(DdUser user)
		{
			SocketGuildUser guildMember = Context.Guild.GetUser(user.DiscordId);
			if (guildMember == null) return false;

			user.Score = await GetUserTimeFromHasmodai(user.LeaderboardId) / 10000;
			Database.FindOneAndUpdate(x => x.DiscordId == user.DiscordId, Builders<DdUser>.Update.Set(x => x.Score, user.Score));

			KeyValuePair<int, ulong> scoreRole = ScoreRoleDictionary.Where(sr => sr.Key <= user.Score).OrderByDescending(sr => sr.Key).FirstOrDefault();
			SocketRole roleToAdd = Context.Guild.GetRole(scoreRole.Value);
			List<SocketRole> removedRoles = await RemoveScoreRolesExcept(guildMember, roleToAdd);

			if (removedRoles.Count == 0 && Helper.MemberHasRole(guildMember, roleToAdd.Id))
				return false;

			EmbedBuilder embed = new EmbedBuilder
			{
				Title = $"Updated roles for {guildMember.Username}",
				Description = $"User: {guildMember.Mention}",
				ThumbnailUrl = guildMember.GetAvatarUrl() ?? guildMember.GetDefaultAvatarUrl()
			};

			if (removedRoles.Count > 0)
				embed.AddField(new EmbedFieldBuilder()
				{
					Name = "Removed:",
					Value = string.Join('\n', removedRoles.Select(sr => sr.Mention)),
					IsInline = true
				});

			if (!Helper.MemberHasRole(guildMember, scoreRole.Value))
			{
				if (roleToAdd != null)
				{
					await guildMember.AddRoleAsync(roleToAdd);
					embed.AddField(new EmbedFieldBuilder()
					{
						Name = "Added:",
						Value = roleToAdd.Mention,
						IsInline = true
					});
				}
				else embed.Description += $"\nFailed to find role from role ID, but it should have been the one for {scoreRole.Key}s+.";
			}

			await ReplyAsync(null, false, embed.Build());
			return true;
		}

		private async Task<int> GetUserTimeFromHasmodai(int userId)
		{
			Dictionary<string, string> postValues = new Dictionary<string, string> { { "uid", userId.ToString() } };

			using FormUrlEncodedContent content = new FormUrlEncodedContent(postValues);
			using HttpClient client = new HttpClient();
			HttpResponseMessage resp = await client.PostAsync("http://dd.hasmodai.com/backend16/get_user_by_id_public.php", content);
			byte[] data = await resp.Content.ReadAsByteArrayAsync();

			int bytePos = 19;
			short usernameLength = BitConverter.ToInt16(data, bytePos);
			bytePos += usernameLength + sizeof(short);
			return BitConverter.ToInt32(data, bytePos + 12);
		}

		public async Task<List<SocketRole>> RemoveScoreRolesExcept(SocketGuildUser member, SocketRole excludedRole)
		{
			List<SocketRole> removedRoles = new List<SocketRole>();

			foreach (var role in member.Roles)
			{
				if (ScoreRoleDictionary.ContainsValue(role.Id) && role.Id != excludedRole.Id)
				{
					await member.RemoveRoleAsync(role);
					removedRoles.Add(role);
				}
			}
			return removedRoles;
		}
	}
}
