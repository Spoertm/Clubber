using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.WebSocket;
using Serilog.Events;

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

    public static async Task ClearMessageComponents(this SocketMessageComponent component)
    {
        if (await component.Channel.GetMessageAsync(component.Message.Id) is IUserMessage originalMessage)
        {
            await originalMessage.ModifyAsync(msg => msg.Components = new ComponentBuilder().Build());
        }
    }

    public static LogEventLevel ToLogEventLevel(this LogSeverity severity) => severity switch
    {
        LogSeverity.Critical => LogEventLevel.Fatal,
        LogSeverity.Error => LogEventLevel.Error,
        LogSeverity.Warning => LogEventLevel.Warning,
        LogSeverity.Info => LogEventLevel.Information,
        LogSeverity.Verbose => LogEventLevel.Verbose,
        LogSeverity.Debug => LogEventLevel.Debug,
        _ => LogEventLevel.Information,
    };
}
