using Discord;

namespace Clubber.Models.Responses
{
	public sealed record DatabaseUpdateResponse(string Message, Embed[] RoleUpdateEmbeds);
}
