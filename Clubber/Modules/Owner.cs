using Clubber.Helpers;
using Discord;
using Discord.Commands;
using System.Diagnostics;
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
			await Program.StopBot();
		}

		[Command("update database")]
		[RequireContext(ContextType.Guild)]
		public async Task UpdateDatabase()
		{
			Stopwatch stopwatch = new();
			stopwatch.Start();

			const string checkingString = "Checking for role updates...";
			IUserMessage msg = await ReplyAsync(checkingString);

			DatabaseUpdateResponse response = await _updateRolesHelper.UpdateRolesAndDb(Context.Guild.Users);
			await msg.ModifyAsync(m => m.Content = $"{checkingString}\n{response.Message}");

			for (int i = 0; i < response.RoleUpdateEmbeds.Length; i++)
				await ReplyAsync(null, false, response.RoleUpdateEmbeds[i]);
		}
	}
}
