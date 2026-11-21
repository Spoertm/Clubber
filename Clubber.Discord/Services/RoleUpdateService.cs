using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.Models;
using Discord;
using Discord.WebSocket;

namespace Clubber.Discord.Services;

/// <summary>Shared `pb` flow: validates the user, then applies the role changes the user is entitled to.</summary>
public sealed class RoleUpdateService(UserService userService, ScoreRoleService scoreRoleService)
{
    public async Task<Result<RoleUpdateOutcome>> PrepareAsync(SocketGuildUser user)
    {
        Result validation = await userService.IsValid(user, true);
        if (validation is Failure validationFailure)
        {
            return validationFailure;
        }

        Result<RoleChange> roleChangeResult = await scoreRoleService.GetRoleChange(user);
        return roleChangeResult switch
        {
            Success<RoleChange> success => await ApplyRoleChangeAsync(user, success.Value),
            Failure roleChangeFailure => Result.Failure<RoleUpdateOutcome>(roleChangeFailure.Message),
        };
    }

    private static async Task<Result<RoleUpdateOutcome>> ApplyRoleChangeAsync(SocketGuildUser user, RoleChange change)
    {
        if (!change.HasChanges)
        {
            return Result.Success(new RoleUpdateOutcome(null, BuildNoChangeMessage(change)));
        }

        if (change.RolesToAdd.Count > 0)
        {
            await user.AddRolesAsync(change.RolesToAdd);
        }

        if (change.RolesToRemove.Count > 0)
        {
            await user.RemoveRolesAsync(change.RolesToRemove);
        }

        return Result.Success(new RoleUpdateOutcome(EmbedHelper.UpdateRoles(new UserRoleUpdate(user, change)), null));
    }

    private static string BuildNoChangeMessage(RoleChange change)
    {
        const string upToDate = "ℹ️ No role changes were needed - your roles are already up to date.";

        return change.SecondsToNextMilestone == 0
            ? upToDate + "\n\nYou already have the highest role in the server!"
            : upToDate + $"\n\nYou're **{change.SecondsToNextMilestone:0.0000}s** away from the next role: {MentionUtils.MentionRole(change.NextRoleId!.Value)}";
    }
}
