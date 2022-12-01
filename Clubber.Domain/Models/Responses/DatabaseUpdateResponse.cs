using Discord;

namespace Clubber.Domain.Models.Responses;

public record struct DatabaseUpdateResponse(string Message, Embed[] RoleUpdateEmbeds);
