using Discord;

namespace Clubber.Models.Responses;

public record struct DatabaseUpdateResponse(string Message, Embed[] RoleUpdateEmbeds);
