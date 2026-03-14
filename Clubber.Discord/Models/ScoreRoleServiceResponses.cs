using Discord;

namespace Clubber.Discord.Models;

public record struct BulkUserRoleUpdates(int NonMemberCount, IReadOnlyCollection<UserRoleUpdate> UserRoleUpdates);

public record struct UserRoleUpdate(IGuildUser User, RoleChange RoleChange);

public record RoleChange(
	IReadOnlyCollection<ulong> RolesToAdd,
	IReadOnlyCollection<ulong> RolesToRemove,
	decimal? SecondsToNextMilestone = null,
	ulong? NextRoleId = null)
{
	public bool HasChanges => RolesToAdd.Count > 0 || RolesToRemove.Count > 0;

	public static RoleChange None(decimal secondsToNext, ulong nextRoleId) =>
		new([], [], secondsToNext, nextRoleId);
}
