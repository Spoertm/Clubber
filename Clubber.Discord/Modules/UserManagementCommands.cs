using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;

namespace Clubber.Discord.Modules;

// ===== SLASH COMMANDS =====
[Name("👤 User Commands")]
public sealed class UserManagementCommands(
    IUserRepository userRepository,
    UserService userService,
    IWebService webService,
    ScoreRoleService scoreRoleService,
    IDiscordHelper discordHelper,
    IOptions<AppConfig> config)
    : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AppConfig _config = config.Value;

    [SlashCommand("register", "Register a user with their Devil Daggers leaderboard ID")]
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task Register(
        [global::Discord.Interactions.Summary("user", "User to register (leave empty for yourself)")]
        SocketGuildUser user,
        [global::Discord.Interactions.Summary("leaderboard-id", "The user's Devil Daggers leaderboard ID")]
        uint lbId)
    {
        await DeferAsync();

        try
        {
            Result result = await userService.IsValidForRegistration(user, lbId, user.Id == Context.User.Id);
            if (result.IsFailure)
            {
                await RespondAsync(result.ErrorMsg, ephemeral: true);
                return;
            }

            Result registrationResult = await userRepository.RegisterAsync(lbId, user.Id);
            if (registrationResult.IsSuccess)
            {
                await user.RemoveRoleAsync(config.Value.NewPalRoleId);
                await user.AddRoleAsync(config.Value.PendingPbRoleId);
                await FollowupAsync("✅ Successfully registered.\n\nDo `+pb` anywhere to get assigned a role.");
            }
            else
            {
                await FollowupAsync($"Failed to execute command: {registrationResult.ErrorMsg}", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            await HandleSlashCommandError(ex);
        }
    }

    [SlashCommand("unregister", "Remove a user from the database")]
    public async Task Unregister(
        [global::Discord.Interactions.Summary("user", "User to unregister (leave empty for yourself)")]
        SocketGuildUser? user = null,
        // Kept as a string: Discord snowflakes exceed Discord's INTEGER option range (max 2^53-1).
        [global::Discord.Interactions.Summary("discord-id", "Discord ID to unregister (use when the user has left the server)")]
        string? discordId = null,
        [global::Discord.Interactions.Summary("leaderboard-id", "Devil Daggers leaderboard ID to unregister")]
        uint? leaderboardId = null)
    {
        try
        {
            int providedIdentifiers = (user is not null ? 1 : 0) + (discordId is not null ? 1 : 0) + (leaderboardId is not null ? 1 : 0);
            if (providedIdentifiers > 1)
            {
                await RespondAsync("Provide only one of `user`, `discord-id`, or `leaderboard-id`.", ephemeral: true);
                return;
            }

            ulong targetDiscordId;
            DdUser? target = null;

            if (user is not null)
            {
                targetDiscordId = user.Id;
            }
            else if (discordId is not null)
            {
                if (!ulong.TryParse(discordId.Trim(), out targetDiscordId))
                {
                    await RespondAsync("That's not a valid Discord ID.", ephemeral: true);
                    return;
                }
            }
            else if (leaderboardId is not null)
            {
                target = await userRepository.FindAsync(leaderboardId.Value);
                if (target is null)
                {
                    await RespondAsync("No user is registered with that leaderboard ID.", ephemeral: true);
                    return;
                }

                targetDiscordId = target.DiscordId;
            }
            else
            {
                targetDiscordId = Context.User.Id;
            }

            bool isSelfCommand = targetDiscordId == Context.User.Id;

            if (!isSelfCommand && !((SocketGuildUser)Context.User).GuildPermissions.ManageRoles)
            {
                await RespondAsync("You can only unregister yourself, or you need ManageRoles permission to unregister others.", ephemeral: true);
                return;
            }

            target ??= await userRepository.FindAsync(targetDiscordId);
            if (target is null)
            {
                await RespondAsync("That user isn't registered.", ephemeral: true);
                return;
            }

            // Self-unregistrations are private; moderator actions on others are public.
            await DeferAsync(ephemeral: isSelfCommand);

            ComponentBuilder components = new ComponentBuilder()
                .WithButton("Confirm", $"unregister-confirm:{Context.User.Id}:{targetDiscordId}", ButtonStyle.Danger)
                .WithButton("Cancel", $"unregister-cancel:{Context.User.Id}", ButtonStyle.Secondary);

            string targetDescription = user is not null ? user.Mention : MentionUtils.MentionUser(targetDiscordId);

            string prompt = $"⚠️ Are you sure you want to unregister {targetDescription}?\n" +
                $"Discord ID: `{targetDiscordId}`\n" +
                $"Leaderboard ID: `{target.LeaderboardId}`";

            await FollowupAsync(prompt, components: components.Build(), ephemeral: isSelfCommand);
        }
        catch (Exception ex)
        {
            await HandleSlashCommandError(ex);
        }
    }

    [ComponentInteraction("unregister-confirm:*:*")]
    public async Task ConfirmUnregister(ulong requesterId, ulong targetDiscordId)
    {
        if (Context.User.Id != requesterId)
        {
            await RespondAsync("Only the user who ran the command can confirm this.", ephemeral: true);
            return;
        }

        bool isSelfCommand = requesterId == targetDiscordId;

        if (!isSelfCommand && !((SocketGuildUser)Context.User).GuildPermissions.ManageRoles)
        {
            await RespondAsync("You need ManageRoles permission to unregister others.", ephemeral: true);
            return;
        }

        try
        {
            await DeferAsync();

            DdUser? ddUser = await userRepository.FindAsync(targetDiscordId);
            if (ddUser is null)
            {
                await UpdateUnregisterPromptAsync("❌ That user isn't registered.");
                return;
            }

            // If they're still in the server, strip their DD roles and give them the unregistered role.
            // If they left, the DB row being gone is enough - UserJoinHandler handles them on rejoin.
            SocketGuildUser? guildUser = discordHelper.GetGuildUser(_config.DdPalsId, targetDiscordId);
            if (guildUser is not null)
            {
                await scoreRoleService.StripDdRolesAsync(guildUser);
            }

            await userRepository.RemoveAsync(targetDiscordId);

            await UpdateUnregisterPromptAsync(
                $"✅ Unregistered {MentionUtils.MentionUser(targetDiscordId)}.\n" +
                $"Discord ID: `{targetDiscordId}`\n" +
                $"Leaderboard ID: `{ddUser.LeaderboardId}`");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error confirming unregister for {TargetDiscordId}", targetDiscordId);
            await UpdateUnregisterPromptAsync("❌ An error occurred while unregistering.");
        }
    }

    [ComponentInteraction("unregister-cancel:*")]
    public async Task CancelUnregister(ulong requesterId)
    {
        if (Context.User.Id != requesterId)
        {
            await RespondAsync("Only the user who ran the command can cancel this.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await UpdateUnregisterPromptAsync("ℹ️ Unregistration cancelled.");
    }

    /// <summary>
    /// Replaces the confirmation prompt with the given result and removes its buttons.
    /// Must be called after <see cref="DeferAsync"/>.
    /// </summary>
    private Task UpdateUnregisterPromptAsync(string message)
    {
        return Context.Interaction.ModifyOriginalResponseAsync(m =>
        {
            m.Content = message;
            m.Components = new ComponentBuilder().Build();
        });
    }

    [SlashCommand("link-twitch", "Link a Twitch account to your Devil Daggers profile on DDLIVE")]
    public async Task LinkTwitch(
        [global::Discord.Interactions.Summary("twitch-username", "Your Twitch username")]
        string twitchUsername,
        [global::Discord.Interactions.Summary("user", "User to link (leave empty for yourself)")]
        SocketGuildUser? user = null)
    {
        await DeferAsync();

        try
        {
            user ??= (SocketGuildUser)Context.User;
            bool isSelfCommand = user.Id == Context.User.Id;

            // Permission check for linking other users
            if (!isSelfCommand && !((SocketGuildUser)Context.User).GuildPermissions.ManageRoles)
            {
                await FollowupAsync("You can only link your own Twitch account, or you need ManageRoles permission to link others.", ephemeral: true);
                return;
            }

            Result result = userService.IsNotBotOrCheater(user, isSelfCommand);
            if (result.IsFailure)
            {
                await FollowupAsync(result.ErrorMsg, ephemeral: true);
                return;
            }

            if (await userRepository.TwitchUsernameExistsAsync(twitchUsername))
            {
                await FollowupAsync("That Twitch username is already registered.", ephemeral: true);
                return;
            }

            Result registrationResult = await userRepository.RegisterTwitchAsync(user.Id, twitchUsername);
            if (registrationResult.IsSuccess)
            {
                await FollowupAsync("✅ Successfully linked Twitch.");
            }
            else
            {
                await FollowupAsync($"Failed to execute command: {registrationResult.ErrorMsg}", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            await HandleSlashCommandError(ex);
        }
    }

    [SlashCommand("unlink-twitch", "Unlink your Twitch account")]
    public async Task UnlinkTwitch(
        [global::Discord.Interactions.Summary("user", "User to unlink (leave empty for yourself)")]
        SocketGuildUser? user = null)
    {
        try
        {
            user ??= (SocketGuildUser)Context.User;

            // Permission check for unlinking other users
            if (user.Id != Context.User.Id && !((SocketGuildUser)Context.User).GuildPermissions.ManageRoles)
            {
                await RespondAsync("You can only unlink your own Twitch account, or you need ManageRoles permission to unlink others.",
                    ephemeral: true);
                return;
            }

            Result result = await userRepository.UnregisterTwitchAsync(user.Id);
            if (result.IsSuccess)
            {
                await RespondAsync("✅ Successfully unlinked Twitch account.", ephemeral: true);
            }
            else
            {
                await RespondAsync($"Failed to execute command: {result.ErrorMsg}", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            await HandleSlashCommandError(ex);
        }
    }

    [SlashCommand("stats", "Get Devil Daggers statistics for a user")]
    public async Task Stats(
        [global::Discord.Interactions.Summary("user", "User to get stats for (leave empty for yourself)")]
        SocketGuildUser? user = null,
        [global::Discord.Interactions.Summary("full", "Show full detailed stats")]
        bool full = false)
    {
        await DeferAsync();

        try
        {
            user ??= (SocketGuildUser)Context.User;

            DdUser? ddUser = await userRepository.FindAsync(user.Id);

            if (ddUser is null)
            {
                Result userValidationResult = await userService.IsValid(user, user.Id == Context.User.Id);
                await FollowupAsync(userValidationResult.ErrorMsg, ephemeral: true);
                return;
            }

            uint leaderboardId = ddUser.LeaderboardId;
            Task<IReadOnlyList<EntryResponse>> playerEntryTask = webService.GetLbPlayers([leaderboardId]);
            Task<GetPlayerHistory?> playerHistoryTask = webService.GetPlayerHistory(leaderboardId);
            await Task.WhenAll(playerEntryTask, playerHistoryTask);

            EntryResponse playerEntry = (await playerEntryTask)[0];
            GetPlayerHistory? playerHistory = await playerHistoryTask;

            Embed statsEmbed;
            MessageComponent? components = null;

            if (full)
            {
                statsEmbed = EmbedHelper.FullStats(playerEntry, user, playerHistory);
            }
            else
            {
                statsEmbed = EmbedHelper.Stats(playerEntry, user, playerHistory);
                ComponentBuilder cb = new();
                cb.WithButton("Full stats", $"stats:{user.Id}:{leaderboardId}");
                components = cb.Build();
            }

            await FollowupAsync(embed: statsEmbed, components: components);
        }
        catch (Exception ex)
        {
            await HandleSlashCommandError(ex);
        }
    }

    [SlashCommand("pb", "Update your Devil Daggers score roles")]
    public async Task UpdateRoles()
    {
        await DeferAsync();

        try
        {
            SocketGuildUser user = (SocketGuildUser)Context.User;
            Result result = await userService.IsValid(user, true);
            if (result.IsFailure)
            {
                await FollowupAsync(result.ErrorMsg, ephemeral: true);
                return;
            }

            Result<RoleChange> roleChangeResult = await scoreRoleService.GetRoleChange(user);
            if (roleChangeResult.IsFailure)
            {
                await FollowupAsync(roleChangeResult.ErrorMsg, ephemeral: true);
                return;
            }

            RoleChange change = roleChangeResult.Value;
            if (change.HasChanges)
            {
                if (change.RolesToAdd.Count > 0)
                    await user.AddRolesAsync(change.RolesToAdd);

                if (change.RolesToRemove.Count > 0)
                    await user.RemoveRolesAsync(change.RolesToRemove);

                await FollowupAsync(embed: EmbedHelper.UpdateRoles(new UserRoleUpdate(user, change)));
            }
            else
            {
                string msg = "No updates were needed.";
                if (change.SecondsToNextMilestone == 0)
                {
                    msg += "\n\nYou already have the highest role in the server!";
                }
                else
                {
                    msg += $"\n\nYou're **{change.SecondsToNextMilestone:0.0000}s** away from the next role: {MentionUtils.MentionRole(change.NextRoleId!.Value)}";
                }

                await FollowupAsync(msg);
            }
        }
        catch (Exception ex)
        {
            await HandleSlashCommandError(ex);
        }
    }

    private async Task HandleSlashCommandError(Exception ex)
    {
        Log.Error(ex, "Slash command error");

        // Try to respond if we haven't already
        const string errorMessage = "An error occurred while processing your command.";
        if (!Context.Interaction.HasResponded)
        {
            await RespondAsync(errorMessage, ephemeral: true);
        }
        else
        {
            // If we already responded, use followup
            await FollowupAsync(errorMessage, ephemeral: true);
        }
    }
}
