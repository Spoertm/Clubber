﻿using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Database")]
	[Group("remove")]
	[Summary("Removes a user from the database.")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	public class RemoveUser : AbstractModule<SocketCommandContext>
	{
		[Command]
		public async Task RemoveByName([Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneGuildUser(name);
			if (success && user != null)
				await ReplyAsync(DatabaseHelper.RemoveUser(user.Id));
		}
	}
}