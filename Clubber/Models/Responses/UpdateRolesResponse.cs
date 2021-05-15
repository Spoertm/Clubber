using Discord.WebSocket;
using System.Collections.Generic;

namespace Clubber.Models.Responses
{
	public sealed record UpdateRolesResponse(bool Success, SocketGuildUser? User, IEnumerable<SocketRole>? RolesAdded, IEnumerable<SocketRole>? RolesRemoved);
}
