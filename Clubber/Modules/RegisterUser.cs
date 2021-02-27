﻿using Clubber.Helpers;
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
		[Remarks("register 118832 chupacabra")]
		[Priority(1)]
		public async Task RegisterByName([Name("Leaderboard ID")] uint lbId, [Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user != null)
				await CheckUserAndRegister(lbId, user);
		}

		[Command("id")]
		[Remarks("register id 118832 222079115849629696")]
		[Priority(2)]
		public async Task RegisterByDiscordId([Name("Leaderboard ID")] uint lbId, [Name("Discord ID")] ulong discordId)
		{
			(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
			if (success && user != null)
				await CheckUserAndRegister(lbId, user);
		}

		private async Task CheckUserAndRegister(uint lbId, SocketGuildUser user)
		{
			if (!await UserIsClean(user, true, true, true, false))
				return;

			await DatabaseHelper.RegisterUser(lbId, user);
			await InlineReplayAsync("✅ Successfully registered.");
		}
	}
}
