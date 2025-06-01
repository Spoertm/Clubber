using Clubber.Domain.Models;
using Discord;

namespace Clubber.Discord.Models;

public record struct BulkUserRoleUpdates(int NonMemberCount, IReadOnlyCollection<UserRoleUpdate> UserRoleUpdates);
public record struct UserRoleUpdate(IGuildUser User, RoleUpdate RoleUpdate);

public abstract class RoleChangeResult
{
	public sealed class None : RoleChangeResult
	{
		private None(decimal secondsAwayFromNextRole, ulong nextRoleId)
		{
			SecondsAwayFromNextRole = secondsAwayFromNextRole;
			NextRoleId = nextRoleId;
		}

		public decimal SecondsAwayFromNextRole { get; }
		public ulong NextRoleId { get; }

		public static None FromMileStoneInfo(MilestoneInfo<ulong> milestoneInfo)
		{
			return new None(milestoneInfo.TimeUntilNextMilestone, milestoneInfo.NextMilestoneId);
		}
	}
}

public class RoleUpdate : RoleChangeResult
{
	private RoleUpdate(IReadOnlyCollection<ulong> rolesToAdd, IReadOnlyCollection<ulong> rolesToRemove)
	{
		RolesToAdd = rolesToAdd;
		RolesToRemove = rolesToRemove;
	}

	public IReadOnlyCollection<ulong> RolesToAdd { get; }
	public IReadOnlyCollection<ulong> RolesToRemove { get; }

	public static RoleUpdate FromCollectionChanges(CollectionChange<ulong> changes)
	{
		return new RoleUpdate(changes.ItemsToAdd, changes.ItemsToRemove);
	}
}
