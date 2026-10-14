using System.Collections.Concurrent;

namespace Clubber.Domain.Helpers;

public enum RegistrationStatus
{
    /// <summary>The user submitted a registration request and is waiting for a moderator.</summary>
    Pending,

    /// <summary>The user was already told that their request is still pending.</summary>
    Reminded,
}

public sealed class RegistrationTracker
{
    private readonly ConcurrentDictionary<ulong, RegistrationStatus> _userRegistrations = new();

    public bool TryFlagUser(ulong userId)
    {
        return _userRegistrations.TryAdd(userId, RegistrationStatus.Pending);
    }

    /// <summary>
    /// Marks a pending user as reminded. Returns <see langword="true"/> only the first time, so
    /// callers can react to the first duplicate message without answering every message.
    /// </summary>
    public bool TryMarkUserReminded(ulong userId)
    {
        return _userRegistrations.TryUpdate(userId, RegistrationStatus.Reminded, RegistrationStatus.Pending);
    }

    public void UnflagUser(ulong userId)
    {
        _userRegistrations.TryRemove(userId, out _);
    }
}
