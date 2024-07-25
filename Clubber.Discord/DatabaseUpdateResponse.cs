using Discord;

namespace Clubber.Discord;

public record struct DatabaseUpdateResponse(string Message, Embed[] RoleUpdateEmbeds);
