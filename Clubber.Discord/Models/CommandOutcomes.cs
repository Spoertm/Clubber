using Discord;

namespace Clubber.Discord.Models;

/// <summary>Reply to send after a role update: an embed when roles changed, otherwise a text message.</summary>
public sealed record RoleUpdateOutcome(Embed? Embed, string? Text);

/// <summary>Reply to send for the stats command.</summary>
public sealed record StatsOutcome(Embed Embed, MessageComponent? Components);
