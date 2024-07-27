using Discord;

namespace Clubber.Discord;

public abstract class UpdateRolesResponse
{
	public sealed class Full : UpdateRolesResponse
	{
		public Full(IGuildUser user, IEnumerable<ulong> rolesAdded, IEnumerable<ulong> rolesRemoved)
		{
			User = user;
			RolesAdded = rolesAdded;
			RolesRemoved = rolesRemoved;
		}

		public IGuildUser User { get; }
		public IEnumerable<ulong> RolesAdded { get; }
		public IEnumerable<ulong> RolesRemoved { get; }
	}

	public sealed class Partial : UpdateRolesResponse
	{
		public Partial(decimal secondsAwayFromNextRole, ulong nextRoleId)
		{
			SecondsAwayFromNextRole = secondsAwayFromNextRole;
			NextRoleId = nextRoleId;
		}

		public decimal SecondsAwayFromNextRole { get; }
		public ulong NextRoleId { get; }
	}
}
