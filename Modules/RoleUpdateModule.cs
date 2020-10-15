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
		[Priority(5)]
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
			foreach (DdUser user in Database.AsQueryable().ToList())
				tasks.Add(UpdateUserRoles(user));

			int usersUpdated = (await Task.WhenAll(tasks)).Count(b => b);
			if (usersUpdated > 0)
				await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and member roles for {usersUpdated} users.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
			else
				await msg.ModifyAsync(m => m.Content = $"No role updates were needed.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
		}

		[Command]
		[Priority(1)]
		public async Task UpdateRoles()
		{
			ulong cheaterRoleId = 693432614727581727;
			var user = Context.User as SocketGuildUser;
			if (user.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{user.Username}, you can't register because you've cheated."); return; }
			if (!Helper.DiscordIdExistsInDb(user.Id, Database)) { await ReplyAsync($"You're not registered, {user.Username}. Please ask an admin/moderator/role assigner to register you."); return; }

			if (!await UpdateUserRoles(Helper.GetDdUserFromId(user.Id, Database)))
				await ReplyAsync($"No updates were needed for you, {user.Username}.");
		}

		[Command]
		[Priority(2)]
		public async Task UpdateRoles([Remainder] string name)
		{
			IEnumerable<IUser> guildMatches = Context.Guild.Users.Where(
				u => u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				(u.Nickname != null && u.Nickname.Contains(name, StringComparison.InvariantCultureIgnoreCase)));

			int guildMatchesCount = guildMatches.Count();

			if (guildMatchesCount == 0) await ReplyAsync($"User not found.");
			else if (guildMatchesCount == 1) await UpdateRolesFromId(guildMatches.First().Id);
			else await ReplyAsync($"Multiple people in the server have `{name.ToLower()}` in their name. Mention the user or specify their ID.");
		}

		[Command]
		[Priority(3)]
		public async Task UpdateRoles(IUser userMention) => await UpdateRolesFromId(userMention.Id);

		[Command("id")]
		[Priority(4)]
		public async Task UpdateRolesFromId(ulong discordId)
		{
			bool userIsInGuild = Context.Guild.GetUser(discordId) != null;
			bool userInDb = Helper.DiscordIdExistsInDb(discordId, Database);
			if (!userIsInGuild && !userInDb) { await ReplyAsync("User not found."); return; }
			if (!userIsInGuild && userInDb) { await ReplyAsync("User is registered but isn't in the server."); return; }

			ulong cheaterRoleId = 693432614727581727;
			var guildUser = Context.Guild.GetUser(discordId);
			if (guildUser.IsBot) { await ReplyAsync($"{guildUser.Mention} is a bot. It can't be registered as a DD player."); return; }
			if (guildUser.Roles.Any(r => r.Id == cheaterRoleId)) { await ReplyAsync($"{guildUser.Username} can't be registered because they've cheated."); return; }
			if (!userInDb) { await ReplyAsync($"`{guildUser.Username}` is not registered. Please ask an admin/moderator/role assigner to register them."); return; }

			if (!await UpdateUserRoles(Helper.GetDdUserFromId(discordId, Database)))
				await ReplyAsync($"No updates were needed for {guildUser.Username}.");
		}

		public async Task<bool> UpdateUserRoles(DdUser user)
		{
			SocketGuildUser guildMember = Context.Guild.GetUser(user.DiscordId);
			if (guildMember == null) return false;

			int newScore = await GetUserTimeFromHasmodai(user.LeaderboardId) / 10000;
			Database.FindOneAndUpdate(x => x.DiscordId == user.DiscordId, Builders<DdUser>.Update.Set(x => x.Score, newScore));

			KeyValuePair<int, ulong> scoreRole = ScoreRoleDictionary.Where(sr => sr.Key <= newScore).OrderByDescending(sr => sr.Key).FirstOrDefault();
			SocketRole roleToAdd = Context.Guild.GetRole(scoreRole.Value);
			List<SocketRole> removedRoles = await RemoveScoreRolesExcept(guildMember, roleToAdd);

			bool memberHasRole = Helper.MemberHasRole(guildMember, roleToAdd.Id);
			if (removedRoles.Count == 0 && memberHasRole) return false;

			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle($"Updated roles for {guildMember.Username}")
				.WithDescription($"User: {guildMember.Mention}")
				.WithThumbnailUrl(guildMember.GetAvatarUrl() ?? guildMember.GetDefaultAvatarUrl());

			if (removedRoles.Count > 0) embed.AddField(new EmbedFieldBuilder()
					.WithName("Removed:")
					.WithValue(string.Join('\n', removedRoles.Select(sr => sr.Mention)))
					.WithIsInline(true));

			if (!memberHasRole)
			{
				await guildMember.AddRoleAsync(roleToAdd);
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Added:")
					.WithValue(roleToAdd.Mention)
					.WithIsInline(true));
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
				if (ScoreRoleDictionary.ContainsValue(role.Id) && role.Id != excludedRole.Id || role.Id == 728663492424499200)
				{
					await member.RemoveRoleAsync(role);
					removedRoles.Add(role);
				}
			}
			return removedRoles;
		}
	}
}
