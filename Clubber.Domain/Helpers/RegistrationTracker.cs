using System.Collections.Concurrent;

namespace Clubber.Domain.Helpers;

public class RegistrationTracker
{
	private readonly ConcurrentDictionary<ulong, bool> _userRegistrations = new();

	public bool UserIsFlagged(ulong userId)
	{
		return _userRegistrations.ContainsKey(userId);
	}

	public void FlagUser(ulong userId)
	{
		_userRegistrations[userId] = true;
	}

	public void UnflagUser(ulong userId)
	{
		_userRegistrations.TryRemove(userId, out _);
	}
}
