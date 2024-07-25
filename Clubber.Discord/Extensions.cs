using Discord;
using System.Diagnostics.CodeAnalysis;

namespace Clubber.Discord;

public static class Extensions
{
	/// <summary>
	/// Returns a sanitized string representing the name of the user in the order Nickname > GlobalName > Username.
	/// </summary>
	[return: NotNullIfNotNull(nameof(guildUser))]
	public static string? AvailableNameSanitized(this IGuildUser? guildUser)
	{
		if (guildUser is null)
		{
			return null;
		}

		return Format.Sanitize(guildUser.Nickname ?? guildUser.GlobalName ?? guildUser.Username);
	}
}
