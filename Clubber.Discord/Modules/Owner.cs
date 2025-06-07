using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using System.Diagnostics;

namespace Clubber.Discord.Modules;

[RequireOwner]
public class Owner(ScoreRoleService scoreRoleService, IDiscordHelper discordHelper) : ExtendedModulebase<SocketCommandContext>
{
	[Command("update database")]
	[RequireContext(ContextType.Guild)]
	public async Task UpdateDatabase()
	{
		const string checkingString = "Checking for role updates...";
		IUserMessage msg = await ReplyAsync(checkingString);

		Stopwatch sw = Stopwatch.StartNew();
		BulkUserRoleUpdates response = await scoreRoleService.GetBulkUserRoleUpdates(Context.Guild.Users);
		sw.Stop();

		string message = response.UserRoleUpdates.Count > 0
			? $"✅ Successfully updated database and {response.UserRoleUpdates.Count} user(s).\n🕐 Execution took {sw.ElapsedMilliseconds} ms."
			: $"No updates needed today.\nExecution took {sw.ElapsedMilliseconds} ms.";

		message += $"\nℹ️ {response.NonMemberCount} user(s) are registered but aren't in the server.";
		await msg.ModifyAsync(m => m.Content = $"{checkingString}\n{message}");

		if (response.UserRoleUpdates.Count == 0)
		{
			return;
		}

		foreach (UserRoleUpdate roleUpdate in response.UserRoleUpdates)
		{
			if (roleUpdate.RoleUpdate.RolesToAdd.Count > 0)
			{
				await roleUpdate.User.AddRolesAsync(roleUpdate.RoleUpdate.RolesToAdd);
			}

			if (roleUpdate.RoleUpdate.RolesToRemove.Count > 0)
			{
				await roleUpdate.User.RemoveRolesAsync(roleUpdate.RoleUpdate.RolesToRemove);
			}
		}

		Embed[] roleUpdateEmbeds = response.UserRoleUpdates
			.Select(EmbedHelper.UpdateRoles)
			.ToArray();

		Result result = await discordHelper.SendEmbedsEfficientlyAsync(roleUpdateEmbeds, Context.Channel.Id);
		if (result.IsFailure)
		{
			await InlineReplyAsync($"Failed to send embeds: {result.ErrorMsg}");
		}
	}

	[Command("welcome")]
	[RequireContext(ContextType.Guild)]
	public async Task PostWelcome() => await Context.Channel.SendMessageAsync(embeds: EmbedHelper.RegisterEmbeds());
}
