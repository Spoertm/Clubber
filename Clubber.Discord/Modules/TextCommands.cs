using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;

namespace Clubber.Discord.Modules;

public sealed class TextCommands(
    RoleUpdateService roleUpdateService,
    UserStatsService userStatsService) : ModuleBase<SocketCommandContext>
{
    [Command("pb")]
    [Summary("Update your Devil Daggers score roles")]
    public async Task UpdateRoles()
    {
        await Context.Channel.TriggerTypingAsync();

        try
        {
            SocketGuildUser user = (SocketGuildUser)Context.User;
            await SendAsync(await roleUpdateService.PrepareAsync(user));
        }
        catch (Exception ex)
        {
            await HandleCommandError(ex);
        }
    }

    [Command("stats")]
    [Alias("statsf", "statsfull")]
    [Summary("Get Devil Daggers statistics for a user")]
    public async Task Stats(SocketGuildUser? user = null)
    {
        await Context.Channel.TriggerTypingAsync();

        try
        {
            user ??= (SocketGuildUser)Context.User;
            bool full = Context.Message.Content.Trim()[1..].StartsWith("statsf", StringComparison.OrdinalIgnoreCase);
            await SendAsync(await userStatsService.GetStatsAsync(user, user.Id == Context.User.Id, full));
        }
        catch (Exception ex)
        {
            await HandleCommandError(ex);
        }
    }

    private Task SendAsync(Result<RoleUpdateOutcome> result) => result switch
    {
        Success<RoleUpdateOutcome> success => SendAsync(success.Value),
        Failure failure => SendAsync(failure.Message),
    };

    private Task SendAsync(Result<StatsOutcome> result) => result switch
    {
        Success<StatsOutcome> success => SendAsync(success.Value),
        Failure failure => SendAsync(failure.Message),
    };

    private Task SendAsync(RoleUpdateOutcome outcome) => outcome.Embed is not null
        ? ReplyAsync(embed: outcome.Embed)
        : ReplyAsync(outcome.Text ?? string.Empty, allowedMentions: AllowedMentions.None, messageReference: new MessageReference(Context.Message.Id));

    private Task SendAsync(StatsOutcome outcome) => ReplyAsync(embed: outcome.Embed, components: outcome.Components);

    private Task SendAsync(string message) => ReplyAsync(message);

    private async Task HandleCommandError(Exception ex)
    {
        Log.Error(ex, "Text command error");
        await ReplyAsync("An error occurred while processing your command.");
    }
}
