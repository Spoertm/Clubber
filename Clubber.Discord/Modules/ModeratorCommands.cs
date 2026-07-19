using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Clubber.Discord.Helpers;
using Clubber.Domain.Configuration;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Data.Entities.DdSplits;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;

namespace Clubber.Discord.Modules;

[Name("🛡️ Moderator Commands")]
[DefaultMemberPermissions(GuildPermission.ManageRoles)]
public sealed partial class ModeratorCommands(
    IOptions<AppConfig> config,
    IDiscordHelper discordHelper,
    IUserRepository userRepository,
        ILeaderboardRepository leaderboardRepository,
    IWebService webService,
    RoleConfigService roleConfigService) : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AppConfig _config = config.Value;
    private readonly RoleConfigService _roleConfigService = roleConfigService;

    [SlashCommand("edit-news", "Edit a DD news post made by the bot")]
    public async Task EditNewsPost()
    {
        try
        {
            SocketTextChannel ddnewsPostChannel = discordHelper.GetTextChannel(_config.DdNewsChannelId);
            IMessage[] messages = (await ddnewsPostChannel.GetMessagesAsync(25).FlattenAsync())
                .Where(m => m.Author.Id == Context.Client.CurrentUser.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .ToArray();

            if (messages.Length == 0)
            {
                await RespondAsync("Couldn't find any recent news posts made by the bot.", ephemeral: true);
                return;
            }

            SelectMenuBuilder menuBuilder = new SelectMenuBuilder()
                .WithCustomId("edit-news-select")
                .WithPlaceholder("Select the message to edit");

            foreach (IMessage message in messages)
            {
                string preview = string.IsNullOrWhiteSpace(message.Content)
                    ? "(no text content)"
                    : ResolveMentions(message.Content.ReplaceLineEndings(" "));

                if (preview.Length > 95)
                {
                    preview = preview[..95] + "…";
                }

                menuBuilder.AddOption(preview, message.Id.ToString(), $"Posted {message.CreatedAt:yyyy-MM-dd HH:mm} UTC");
            }

            ComponentBuilder componentBuilder = new ComponentBuilder().WithSelectMenu(menuBuilder);
            await RespondAsync("Select the news post to edit:", components: componentBuilder.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            await RespondAsync("Failed to fetch news posts.", ephemeral: true);
            Log.Error(ex, "Error fetching news posts");
        }
    }

    [ComponentInteraction("edit-news-select")]
    public async Task EditNewsPostSelect(string[] values)
    {
        try
        {
            ulong messageId = ulong.Parse(values[0]);
            SocketTextChannel ddnewsPostChannel = discordHelper.GetTextChannel(_config.DdNewsChannelId);
            if (await ddnewsPostChannel.GetMessageAsync(messageId) is not IUserMessage messageToEdit)
            {
                await RespondAsync("Could not find that message.", ephemeral: true);
                return;
            }

            ModalBuilder modalBuilder = new ModalBuilder()
                .WithTitle("Edit news post")
                .WithCustomId($"edit-news-modal:{messageId}")
                .AddTextInput(
                    label: "Content",
                    customId: "edit-news-content",
                    style: TextInputStyle.Paragraph,
                    placeholder: "New content for the news post",
                    maxLength: 2000,
                    value: string.IsNullOrEmpty(messageToEdit.Content) ? null : messageToEdit.Content);

            await RespondWithModalAsync(modalBuilder.Build());
        }
        catch (Exception ex)
        {
            await RespondAsync("Failed to open editor.", ephemeral: true);
            Log.Error(ex, "Error opening news post editor");
        }
    }

    [ModalInteraction("edit-news-modal:*")]
    public async Task EditNewsPostModal(ulong messageId)
    {
        SocketModal modal = (SocketModal)Context.Interaction;
        string newContent = modal.Data.Components.First(x => x.CustomId == "edit-news-content").Value;

        if (string.IsNullOrWhiteSpace(newContent))
        {
            await RespondAsync("Message content cannot be empty.", ephemeral: true);
            return;
        }

        try
        {
            SocketTextChannel ddnewsPostChannel = discordHelper.GetTextChannel(_config.DdNewsChannelId);
            if (await ddnewsPostChannel.GetMessageAsync(messageId) is not IUserMessage messageToEdit)
            {
                await RespondAsync("Could not find that message.", ephemeral: true);
                return;
            }

            await messageToEdit.ModifyAsync(m => m.Content = newContent);
            await RespondAsync("✅ Message updated successfully!", ephemeral: true);
        }
        catch (Exception ex)
        {
            await RespondAsync("Failed to edit message.", ephemeral: true);
            Log.Error(ex, "Error editing news post");
        }
    }

    private string ResolveMentions(string content)
    {
        return MentionRegex().Replace(content, match =>
        {
            if (ulong.TryParse(match.Groups[1].Value, out ulong userId) &&
                Context.Guild?.GetUser(userId) is { } user)
            {
                return $"@{user.AvailableNameSanitized()}";
            }

            return match.Value;
        });
    }

    [SlashCommand("clear-register", "Clear all messages but the first one in register channel")]
    public async Task ClearRegisterChannel()
    {
        ulong registerChannelId = _config.RegisterChannelId;
        if (Context.Channel.Id != registerChannelId)
        {
            await RespondAsync($"This command can only be run in <#{registerChannelId}>.", ephemeral: true);
            return;
        }

        await DeferAsync();

        try
        {
            SocketTextChannel channel = (SocketTextChannel)Context.Channel;
            await discordHelper.ClearChannelAsync(channel);
            IReadOnlyDictionary<int, ulong> scoreRoles = await _roleConfigService.GetScoreRolesAsync();
            await channel.SendMessageAsync(embeds: EmbedHelper.RegisterEmbeds(scoreRoles));
            await FollowupAsync("✅ Register channel cleared and reset!");
        }
        catch (Exception ex)
        {
            await FollowupAsync("Failed to clear register channel.", ephemeral: true);
            Log.Error(ex, "Error clearing register channel");
        }
    }

    [SlashCommand("check-splits", "Check if a ddstats run has better splits and update if necessary")]
    public async Task CheckSplits(
        [global::Discord.Interactions.Summary("url-or-id", "ddstats URL or run ID")]
        string urlOrId,
        [global::Discord.Interactions.Summary("split-name", "Specific split to check (e.g. 350, 700, etc.)")]
        int? splitName = null,
        [global::Discord.Interactions.Summary("description", "Description for the run")]
        string? description = null)
    {
        await DeferAsync();

        try
        {
            Uri? ddstatsUri = null;

            if (uint.TryParse(urlOrId, out uint runId))
            {
                ddstatsUri = new Uri($"https://ddstats.com/api/v2/game/full?id={runId}");
            }
            else if (Uri.TryCreate(urlOrId, UriKind.Absolute, out Uri? parsedUri))
            {
                ddstatsUri = parsedUri;
            }

            if (ddstatsUri == null)
            {
                await FollowupAsync("Invalid URL or run ID.", ephemeral: true);
                return;
            }

            DdStatsFullRunResponse ddstatsRun = await webService.GetDdstatsResponse(ddstatsUri);

            if (!ddstatsRun.GameInfo.Spawnset.Equals("v3", StringComparison.OrdinalIgnoreCase))
            {
                await FollowupAsync("This is not a V3 run.", ephemeral: true);
                return;
            }

            string desc = description ?? $"{ddstatsRun.GameInfo.PlayerName} {ddstatsRun.GameInfo.GameTime:0.0000}";
            IReadOnlyCollection<Split> splits = RunAnalyzer.GetData(ddstatsRun);

            if (splits.Any(s => s.Value > 1000))
            {
                await FollowupAsync("Invalid run: too many homings gained on some splits.", ephemeral: true);
                return;
            }

            (BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits) response;

            if (splitName.HasValue)
            {
                string splitNameStr = splitName.Value.ToString();
                bool splitExists = Split.V3Splits.Any(s => s.Name == splitNameStr);
                if (!splitExists)
                {
                    await FollowupAsync($"The split `{splitNameStr}` doesn't exist.", ephemeral: true);
                    return;
                }

                Split? split = splits.FirstOrDefault(s => s.Name == splitNameStr);
                if (split == null)
                {
                    await FollowupAsync($"The split `{splitNameStr}` isn't in the run.", ephemeral: true);
                    return;
                }

                if (split.Value > 1000)
                {
                    await FollowupAsync("Invalid run: too many homings gained on that split.", ephemeral: true);
                    return;
                }

                response = await leaderboardRepository.UpdateBestSplitsAsync([split], ddstatsRun, desc);
            }
            else
            {
                response = await leaderboardRepository.UpdateBestSplitsAsync(splits, ddstatsRun, desc);
            }

            if (response.UpdatedBestSplits.Length == 0)
            {
                await FollowupAsync("No updates were needed.");
                return;
            }

            Embed updatedSplitsEmbed = EmbedHelper.UpdatedSplits(response.OldBestSplits, response.UpdatedBestSplits);
            await FollowupAsync(embed: updatedSplitsEmbed);
        }
        catch (Exception ex)
        {
            string errorMsg = ex switch
            {
                ClubberException => ex.Message,
                HttpRequestException => "Couldn't fetch run data. Either the provided run ID doesn't exist or ddstats servers are down.",
                SerializationException => "Couldn't read ddstats run data.",
                _ => "Internal error.",
            };

            Log.Error(ex, "Error checking splits");
            await FollowupAsync($"Failed to check splits: {errorMsg}", ephemeral: true);
        }
    }

    [SlashCommand("check-homing-peak", "Check if a ddstats run has better homing peak and update if necessary")]
    public async Task CheckHomingPeak(
        [global::Discord.Interactions.Summary("url-or-id", "ddstats URL or run ID")]
        string urlOrId)
    {
        await DeferAsync();

        try
        {
            Uri? ddstatsUri = null;

            if (uint.TryParse(urlOrId, out uint runId))
            {
                ddstatsUri = new Uri($"https://ddstats.com/api/v2/game/full?id={runId}");
            }
            else if (Uri.TryCreate(urlOrId, UriKind.Absolute, out Uri? parsedUri))
            {
                ddstatsUri = parsedUri;
            }

            if (ddstatsUri == null)
            {
                await FollowupAsync("Invalid URL or run ID.", ephemeral: true);
                return;
            }

            DdStatsFullRunResponse ddStatsRun = await webService.GetDdstatsResponse(ddstatsUri);

            if (!ddStatsRun.GameInfo.Spawnset.Equals("v3", StringComparison.OrdinalIgnoreCase))
            {
                await FollowupAsync("It has to be a v3 run.", ephemeral: true);
                return;
            }

            const int homingPeakLimit = 1500;
            if (ddStatsRun.GameInfo.HomingDaggersMax > homingPeakLimit)
            {
                await FollowupAsync($"Invalid run: the homing peak is unrealistically high (>{homingPeakLimit}).", ephemeral: true);
                return;
            }

            HomingPeakRun possibleNewTopPeakRun = new()
            {
                PlayerName = ddStatsRun.GameInfo.PlayerName,
                PlayerLeaderboardId = ddStatsRun.GameInfo.PlayerId,
                HomingPeak = ddStatsRun.GameInfo.HomingDaggersMax,
                Source = $"https://ddstats.com/games/{ddStatsRun.GameInfo.Id}",
            };

            (HomingPeakRun? OldRun, HomingPeakRun? NewRun) = await leaderboardRepository.UpdateTopHomingPeakAsync(possibleNewTopPeakRun);
            if (NewRun is null)
            {
                await FollowupAsync("No updates were needed.");
                return;
            }

            string userName = ddStatsRun.GameInfo.PlayerName;
            string? avatarUrl = null;
            DdUser? ddUser = await userRepository.FindAsync(ddStatsRun.GameInfo.PlayerId);
            if (ddUser != null && Context.Guild.GetUser(ddUser.DiscordId) is { } user)
            {
                userName = user.AvailableNameSanitized();
                avatarUrl = user.GetDisplayAvatarUrl() ?? user.GetDefaultAvatarUrl();
            }

            Embed updatedPeakEmbed = EmbedHelper.UpdateTopPeakRuns(userName, NewRun, OldRun, avatarUrl);
            await FollowupAsync(embed: updatedPeakEmbed);
        }
        catch (Exception ex)
        {
            string errorMsg = ex switch
            {
                ClubberException => ex.Message,
                HttpRequestException => "Couldn't fetch run data. Either the provided run ID doesn't exist or ddstats servers are down.",
                SerializationException => "Couldn't read ddstats run data.",
                _ => "Internal error.",
            };

            Log.Error(ex, "Error checking homing peak");
            await FollowupAsync($"Failed to check homing peak: {errorMsg}", ephemeral: true);
        }
    }

    [GeneratedRegex(@"<@!?(\d+)>")]
    private static partial Regex MentionRegex();
}
