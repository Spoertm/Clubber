using Discord;

namespace Clubber.Discord.Models;

public record struct DatabaseUpdateResponse(string Message, Embed[] RoleUpdateEmbeds);
