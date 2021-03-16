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
		[Command]
		[Remarks("pb")]
		[Priority(1)]
		public async Task UpdateRolesFromCurrentUser() => await CheckUserAndUpdateRoles(Context.Guild.GetUser(Context.User.Id));

		[Command]
		[Remarks("pb clubber\npb <@743431502842298368>")]
		[Priority(2)]
		public async Task UpdateRolesFromName([Name("name | tag")][Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user != null)
				await CheckUserAndUpdateRoles(user);
		}

		[Command("id")]
		[Remarks("pb id 743431502842298368")]
		[Priority(3)]
		public async Task UpdateRolesFromDiscordId([Name("Discord ID")] ulong discordId)
		{
			(bool success, SocketGuildUser? user) = await FoundUserFromDiscordId(discordId);
			if (success && user != null)
				await CheckUserAndUpdateRoles(user);
		}

		private async Task CheckUserAndUpdateRoles(SocketGuildUser user)
		{
			if (!await UserIsClean(user, true, true, false, true))
				return;

			UpdateRolesResponse response = await UpdateRolesHelper.UpdateUserRoles(user);

			if (!response.Success)
				await InlineReplyAsync("No updates were needed.");
			else
				await ReplyAsync(null, false, UpdateRolesHelper.GetUpdateRolesEmbed(response), null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}

		[Command("database")]
		[Priority(4)]
		[RequireOwner]
		public async Task UpdateDatabase()
		{
			Stopwatch stopwatch = new();
			stopwatch.Start();

			IUserMessage msg = await ReplyAsync("Processing...");

			DatabaseUpdateResponse response = await UpdateRolesHelper.UpdateRolesAndDb(Context.Guild.Users);
			long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

			if (response.NonMemberCount > 0)
				await ReplyAsync($"ℹ️ Unable to update {response.NonMemberCount} user(s) because they're not in the server.");

			int updatedUsers = 0;
			foreach (UpdateRolesResponse updateResponse in response.UpdateResponses.Where(ur => ur.Success))
			{
				await ReplyAsync(null, false, UpdateRolesHelper.GetUpdateRolesEmbed(updateResponse));
				updatedUsers++;
			}

			if (updatedUsers > 0)
				await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and {updatedUsers} user(s).\n🕐 Execution took {elapsedMilliseconds} ms.");
			else
				await msg.ModifyAsync(m => m.Content = $"No updates needed today.\nExecution took {elapsedMilliseconds} ms.");
		}
	}
}
