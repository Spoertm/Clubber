using Clubber.Domain.Models;
using Discord;

namespace Clubber.Discord.Models;

public readonly record struct BulkUserRoleUpdates(int NonMemberCount, IReadOnlyCollection<UserRoleUpdate> UserRoleUpdates);
public readonly record struct UserRoleUpdate(IGuildUser User, RoleUpdate RoleUpdate);

public abstract record RoleChangeResult
{
	public sealed record None(decimal SecondsAwayFromNextRole, ulong NextRoleId) : RoleChangeResult
	{
		public static None FromMileStoneInfo(MilestoneInfo<ulong> milestoneInfo)
		{
			return new(milestoneInfo.TimeUntilNextMilestone, milestoneInfo.NextMilestoneId);
		}
	}
}

public sealed record RoleUpdate(IReadOnlyCollection<ulong> RolesToAdd, IReadOnlyCollection<ulong> RolesToRemove) : RoleChangeResult
{
	public static RoleUpdate FromCollectionChanges(CollectionChange<ulong> changes)
	{
		return new(changes.ItemsToAdd, changes.ItemsToRemove);
	}
}
