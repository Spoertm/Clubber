using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Roles")]
	[Group("pb")]
	[Alias("updateroles")]
	[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles.")]
	public class UpdateRoles : AbstractModule<SocketCommandContext>
	{
		[Command("database")]
		[Priority(4)]
		[RequireOwner]
		public async Task UpdateDatabase()
		{
			Stopwatch stopwatch = new();
			stopwatch.Start();

			IUserMessage msg = await ReplyAsync("Processing...");

			DatabaseUpdateResponse response = await UpdateRolesHelper.UpdateRolesAndDb(Context.Guild);
			long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

			if (response.NonMemberCount > 0)
				await ReplyAsync($"ℹ️ Unable to update {response.NonMemberCount} user(s) because they're not in the server.");

			if (response.UpdatedUsers > 0)
			{
				foreach (UpdateRolesResponse updateResponse in response.UpdateResponses.Where(ur => ur.Success))
					await ReplyAsync(null, false, UpdateRolesHelper.GetUpdateRolesEmbed(updateResponse));

				await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and {response.UpdatedUsers} user(s).\n🕐 Execution took {elapsedMilliseconds} ms.");
			}
			else
			{
				await msg.ModifyAsync(m => m.Content = $"No updates needed today.\nExecution took {elapsedMilliseconds} ms");
			}
		}

		private async Task CheckUserAndUpdateRoles(SocketGuildUser user)
		{
			if (!await UserIsClean(user, true, true, false, true))
				return;

			UpdateRolesResponse response = await UpdateRolesHelper.UpdateUserRoles(user);

			if (!response.Success)
				await InlineReplayAsync("No updates were needed.");
			else
				await ReplyAsync(null, false, UpdateRolesHelper.GetUpdateRolesEmbed(response), null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}

		[Command("id")]
		[Priority(3)]
		public async Task UpdateRolesFromDiscordId(ulong discordId)
		{
			(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
			if (success && user != null)
				await CheckUserAndUpdateRoles(user);
		}

		[Command]
		[Priority(2)]
		public async Task UpdateRolesFromName([Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user != null)
				await CheckUserAndUpdateRoles(user);
		}

		[Command]
		[Priority(1)]
		public async Task UpdateRolesFromCurrentUser() => await CheckUserAndUpdateRoles(Context.Guild.GetUser(Context.User.Id));
	}
}
