using Discord;
using System.Collections.Generic;

namespace Clubber.Models.Responses
{
	public sealed record UpdateRolesResponse(bool Success, IGuildUser? User, IEnumerable<ulong>? RolesAdded, IEnumerable<ulong>? RolesRemoved);
}
