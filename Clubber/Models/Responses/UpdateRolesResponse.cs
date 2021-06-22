using Discord.WebSocket;
using System.Collections.Generic;

namespace Clubber.Models.Responses
{
	public sealed record UpdateRolesResponse(bool Success, SocketGuildUser? User, IEnumerable<ulong>? RolesAdded, IEnumerable<ulong>? RolesRemoved);
}
