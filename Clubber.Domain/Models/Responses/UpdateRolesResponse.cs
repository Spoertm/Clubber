using Discord;

namespace Clubber.Domain.Models.Responses;

public record struct UpdateRolesResponse(bool Success, IGuildUser? User, IEnumerable<ulong>? RolesAdded, IEnumerable<ulong>? RolesRemoved);
