using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;

namespace Clubber.Discord.Modules;

public sealed class TextCommands(
	IDatabaseHelper databaseHelper,
	UserService userService,
	IWebService webService,
	ScoreRoleService scoreRoleService) : ModuleBase<SocketCommandContext>
{
	[Command("pb")]
	[Summary("Update your Devil Daggers score roles")]
	public async Task UpdateRoles()
	{
		try
		{
			SocketGuildUser user = (SocketGuildUser)Context.User;
			Result result = await userService.IsValid(user, true);
			if (result.IsFailure)
			{
				await ReplyAsync(result.ErrorMsg);
				return;
			}

			Result<RoleChange> roleChangeResult = await scoreRoleService.GetRoleChange(user);
			if (roleChangeResult.IsFailure)
			{
				await ReplyAsync(roleChangeResult.ErrorMsg);
				return;
			}

			RoleChange change = roleChangeResult.Value;
			if (change.HasChanges)
			{
				if (change.RolesToAdd.Count > 0)
					await user.AddRolesAsync(change.RolesToAdd);

				if (change.RolesToRemove.Count > 0)
					await user.RemoveRolesAsync(change.RolesToRemove);

				await ReplyAsync(embed: EmbedHelper.UpdateRoles(new UserRoleUpdate(user, change)));
			}
			else
			{
				string msg = "No updates were needed.";
				if (change.SecondsToNextMilestone == 0)
				{
					msg += "\n\nYou already have the highest role in the server!";
				}
				else
				{
					msg += $"\n\nYou're **{change.SecondsToNextMilestone:0.0000}s** away from the next role: {MentionUtils.MentionRole(change.NextRoleId!.Value)}";
				}

				await ReplyAsync(msg, allowedMentions: AllowedMentions.None, messageReference: new MessageReference(Context.Message.Id));
			}
		}
		catch (Exception ex)
		{
			await HandleCommandError(ex);
		}
	}

	[Command("stats")]
	[Alias("statsf", "statsfull")]
	[Summary("Get Devil Daggers statistics for a user")]
	public async Task Stats(SocketGuildUser? user = null)
	{
		try
		{
			user ??= (SocketGuildUser)Context.User;

			DdUser? ddUser = await databaseHelper.FindRegisteredUser(user.Id);

			if (ddUser is null)
			{
				Result userValidationResult = await userService.IsValid(user, user.Id == Context.User.Id);
				await ReplyAsync(userValidationResult.ErrorMsg);
				return;
			}

			uint leaderboardId = (uint)ddUser.LeaderboardId;
			Task<IReadOnlyList<EntryResponse>> playerEntryTask = webService.GetLbPlayers([leaderboardId]);
			Task<GetPlayerHistory?> playerHistoryTask = webService.GetPlayerHistory(leaderboardId);
			await Task.WhenAll(playerEntryTask, playerHistoryTask);

			EntryResponse playerEntry = (await playerEntryTask)[0];
			GetPlayerHistory? playerHistory = await playerHistoryTask;

			string userMsg = Context.Message.Content.Trim()[1..];
			if (userMsg.StartsWith("statsf", StringComparison.OrdinalIgnoreCase))
			{
				Embed statsEmbed = EmbedHelper.FullStats(playerEntry, user, playerHistory);
				await ReplyAsync(embed: statsEmbed);
			}
			else
			{
				Embed statsEmbed = EmbedHelper.Stats(playerEntry, user, playerHistory);
				ComponentBuilder cb = new();
				cb.WithButton("Full stats", $"stats:{user.Id}:{leaderboardId}");
				await ReplyAsync(embed: statsEmbed, components: cb.Build());
			}
		}
		catch (Exception ex)
		{
			await HandleCommandError(ex);
		}
	}

	private async Task HandleCommandError(Exception ex)
	{
		Log.Error(ex, "Text command error");
		await ReplyAsync("An error occurred while processing your command.");
	}
}
