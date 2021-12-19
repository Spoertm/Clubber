using Discord;
using System.Collections.Generic;

namespace Clubber.Models.Responses
{
	public record struct UpdateRolesResponse(bool Success, IGuildUser? User, IEnumerable<ulong>? RolesAdded, IEnumerable<ulong>? RolesRemoved);
}
