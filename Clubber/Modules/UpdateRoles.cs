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
	[Group("updateroles")]
	[Summary("Updates your own roles if nothing is specified. Otherwise a specific user's roles based on the input type.")]
	public class UpdateRoles : AbstractModule<SocketCommandContext>
	{
		[Command("database")]
		[Priority(4)]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public async Task UpdateDatabase()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			IUserMessage msg = await ReplyAsync("Processing...");

			DatabaseUpdateResponse response = await UpdateRolesHelper.UpdateRolesAndDb(Context.Guild);
			long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

			if (response.NonMemberCount > 0)
				await ReplyAsync($"ℹ️ Unable to update {response.NonMemberCount} user(s) because they're not in the server.");

			if (response.UpdatedUsers > 0)
			{
				foreach (UpdateRolesResponse updateResponse in response.UpdateResponses)
				{
					if (updateResponse.Success)
						await WriteRoleUpdateEmbed(updateResponse);
				}

				await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and {response.UpdatedUsers} user(s).\n🕐 Execution took {elapsedMilliseconds} ms");
			}
			else
			{
				await msg.ModifyAsync(m => m.Content = $"No updated needed today.\nExecution took {elapsedMilliseconds} ms");
			}
		}

		[Command]
		[Priority(3)]
		public async Task UpdateRolesFromMention(SocketGuildUser user)
		{
			if (!await UserIsClean(user, true, false, false, true))
				return;

			UpdateRolesResponse response = await UpdateRolesHelper.UpdateUserRoles(user);

			if (!response.Success)
				await ReplyAsync("No updates were needed.");
			else
				await WriteRoleUpdateEmbed(response);
		}

		[Command]
		[Priority(2)]
		public async Task UpdateRolesFromName([Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneGuildUser(name);
			if (success && user != null)
				await UpdateRolesFromMention(user);
		}

		[Command]
		[Priority(1)]
		public async Task UpdateRolesFromCurrentUser() => await UpdateRolesFromMention(Context.Guild.GetUser(Context.User.Id));

		private async Task WriteRoleUpdateEmbed(UpdateRolesResponse response)
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle($"Updated roles for {response.User!.Username}")
				.WithDescription($"User: {response.User!.Mention}")
				.WithThumbnailUrl(response.User!.GetAvatarUrl() ?? response.User!.GetDefaultAvatarUrl());

			if (response.RolesRemoved!.Any())
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Removed:")
					.WithValue(string.Join('\n', response.RolesRemoved!.Select(rr => rr.Mention)))
					.WithIsInline(true));
			}

			if (response.RolesAdded!.Any())
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Added:")
					.WithValue(string.Join('\n', response.RolesAdded!.Select(ar => ar.Mention)))
					.WithIsInline(true));
			}

			await ReplyAsync(null, false, embed.Build());
		}
	}
}
