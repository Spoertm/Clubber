﻿using Clubber.Helpers;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[RequireOwner]
	public class Owner : ExtendedModulebase<SocketCommandContext>
	{
		private readonly UpdateRolesHelper _updateRolesHelper;

		public Owner(UpdateRolesHelper updateRolesHelper)
		{
			_updateRolesHelper = updateRolesHelper;
		}

		[Command("stopbot")]
		public async Task StopBot()
		{
			await ReplyAsync("Exiting...");
			Program.StopBot();
		}

		[Command("update database")]
		[RequireContext(ContextType.Guild)]
		public async Task UpdateDatabase()
		{
			const string checkingString = "Checking for role updates...";
			IUserMessage msg = await ReplyAsync(checkingString);

			(string message, Embed[] roleUpdateEmbeds) = await _updateRolesHelper.UpdateRolesAndDb(Context.Guild.Users);
			await msg.ModifyAsync(m => m.Content = $"{checkingString}\n{message}");

			for (int i = 0; i < roleUpdateEmbeds.Length; i++)
				await ReplyAsync(null, false, roleUpdateEmbeds[i]);
		}
	}
}
