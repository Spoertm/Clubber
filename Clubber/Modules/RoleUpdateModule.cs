using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Clubber.Helpers.UpdateRolesHelper;

namespace Clubber.Modules
{
	[Name("Roles")]
	[Group("updateroles")]
	[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
	public class RoleUpdateModule : AbstractModule<SocketCommandContext>
	{
		private readonly UpdateRolesHelper _updateRolesHelper;
		private readonly DatabaseHelper _databaseHelper;

		public RoleUpdateModule(UpdateRolesHelper updateRolesHelper, DatabaseHelper databaseHelper)
		{
			_updateRolesHelper = updateRolesHelper;
			_databaseHelper = databaseHelper;
		}

		[Command("database")]
		[Priority(5)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task UpdateRolesAndDataBase()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			IUserMessage msg = await ReplyAsync("Processing...");

			UpdateRolesResponse response = await _updateRolesHelper.UpdateRolesAndDb();

			if (response.NonMemberCount > 0)
				await ReplyAsync($"ℹ️ Unable to update {response.NonMemberCount} user(s). They're most likely not in the server.");

			if (response.UpdatedUsers > 0)
				await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and member roles for {response.UpdatedUsers} users.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
			else
				await msg.ModifyAsync(m => m.Content = $"No role updates were needed.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
		}

		[Command]
		[Priority(1)]
		public async Task UpdateRoles()
		{
			SocketGuildUser user = Context.User as SocketGuildUser;
			if (await IsError(user.Roles.Any(r => r.Id == Constants.CheaterRoleId), $"{user.Username}, you can't register because you've cheated.") ||
				await IsError(!_databaseHelper.DiscordIdExistsInDb(user.Id), $"You're not registered, {user.Username}. Please ask an admin/moderator/role assigner to register you."))
			{
				return;
			}

			UpdateRoleResponse response = await _updateRolesHelper.UpdateUserRoles(_databaseHelper.GetDdUserFromId(user.Id));

			if (!response.Success)
				await ReplyAsync($"No updates were needed for you, {user.Username}.");
			else
				await WriteRoleUpdateEmbed(response);
		}

		public async Task WriteRoleUpdateEmbed(UpdateRoleResponse response)
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle($"Updated roles for {response.GuildMember.Username}")
				.WithDescription($"User: {response.GuildMember.Mention}")
				.WithThumbnailUrl(response.GuildMember.GetAvatarUrl() ?? response.GuildMember.GetDefaultAvatarUrl());

			if (response.RemovedRoles.Count > 0)
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Removed:")
					.WithValue(string.Join('\n', response.RemovedRoles.Select(rr => rr.Mention)))
					.WithIsInline(true));
			}

			if (response.AddedRoles.Count > 0)
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Added:")
					.WithValue(string.Join('\n', response.AddedRoles.Select(ar => ar.Mention)))
					.WithIsInline(true));
			}

			await ReplyAsync(null, false, embed.Build());
		}

		[Command]
		[Priority(2)]
		public async Task UpdateRoles([Remainder] string name)
		{
			IEnumerable<IUser> guildMatches = Context.Guild.Users.Where(u =>
				u.Username.Contains(name, StringComparison.InvariantCultureIgnoreCase) ||
				u.Nickname?.Contains(name, StringComparison.InvariantCultureIgnoreCase) == true);

			int guildMatchesCount = guildMatches.Count();

			if (guildMatchesCount == 0)
				await ReplyAsync("User not found.");
			else if (guildMatchesCount == 1)
				await UpdateRolesFromId(guildMatches.First().Id);
			else
				await ReplyAsync($"Multiple people in the server have `{name.ToLower()}` in their name. Mention the user or specify their ID.");
		}

		[Command]
		[Priority(3)]
		public async Task UpdateRoles(IUser userMention)
			=> await UpdateRolesFromId(userMention.Id);

		[Command("id")]
		[Priority(4)]
		public async Task UpdateRolesFromId(ulong discordId)
		{
			bool userIsInGuild = Context.Guild.GetUser(discordId) != null;
			bool userInDb = _databaseHelper.DiscordIdExistsInDb(discordId);
			if (await IsError(!userIsInGuild && !userInDb, "User not found.") || await IsError(!userIsInGuild && userInDb, "User is registered but isn't in the server."))
				return;

			SocketGuildUser guildUser = Context.Guild.GetUser(discordId);
			if (await IsError(guildUser.IsBot, $"{guildUser.Mention} is a bot. It can't be registered as a DD player.") ||
				await IsError(guildUser.Roles.Any(r => r.Id == Constants.CheaterRoleId), $"{guildUser.Username} can't be registered because they've cheated.") ||
				await IsError(!userInDb, $"`{guildUser.Username}` is not registered. Please ask an admin/moderator/role assigner to register them."))
			{
				return;
			}

			UpdateRoleResponse response = await _updateRolesHelper.UpdateUserRoles(_databaseHelper.GetDdUserFromId(discordId));
			if (!response.Success)
				await ReplyAsync($"No updates were needed for {guildUser.Username}.");
			else
				await WriteRoleUpdateEmbed(response);
		}
	}
}
