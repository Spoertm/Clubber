using System.Runtime.Serialization;
using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Clubber.Domain.Extensions;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Clubber.Discord.Services;

public sealed class RegistrationRequestHandler
{
    // Sentinel leaderboard ID for a "no score" registration request. It must be a non-negative
    // number so the Confirm button's wildcard capture parses back into a uint.
    public const uint NoScoreLeaderboardId = 0;

    private readonly AppConfig _config;
    private readonly RegistrationTracker _registrationTracker;
    private readonly IWebService _webService;
    private readonly IDiscordHelper _discordHelper;
    private readonly IUserRepository _userRepository;
    private readonly ClubberDiscordClient _discordClient;

    public RegistrationRequestHandler(
        IOptions<AppConfig> config,
        RegistrationTracker registrationTracker,
        IWebService webService,
        IDiscordHelper discordHelper,
        ClubberDiscordClient clubberDiscordClient,
        IUserRepository userRepository)
    {
        _config = config.Value;
        _registrationTracker = registrationTracker;
        _webService = webService;
        _discordHelper = discordHelper;
        _userRepository = userRepository;
        _discordClient = clubberDiscordClient;
    }

    public Task InitializeAsync()
    {
        _discordClient.MessageReceived += message => Task.Run(() => Handle(message));
        return Task.CompletedTask;
    }

    private async Task Handle(SocketMessage socketMessage)
    {
        if (!(socketMessage is SocketUserMessage { Source: MessageSource.User } message && message.Channel.Id == _config.RegisterChannelId))
        {
            return;
        }

        if (await _userRepository.FindAsync(message.Author.Id) != null)
        {
            return;
        }

        uint? foundId = message.Content.FindFirstUint();
        bool containsNoScore = message.Content.Contains("no score", StringComparison.OrdinalIgnoreCase);

        // Unrelated chatter in the register channel is ignored.
        if (foundId is null && !containsNoScore)
        {
            return;
        }

        if (!_registrationTracker.TryFlagUser(message.Author.Id))
        {
            // The user already has a pending registration request. Remind them only once, then
            // stay silent so that every additional message doesn't get a reply.
            if (_registrationTracker.TryMarkUserReminded(message.Author.Id))
            {
                await message.ReplyAsync(embed: new EmbedBuilder().WithDescription("ℹ️ You've already provided an ID. Mods will register you soon.").Build());
            }

            return;
        }

        Embed registerEmbed;
        ComponentBuilder cb = new();

        // User specified an ID
        if (foundId is uint id)
        {
            Result<EntryResponse> playerInfoResponse = await GetEntryResponse(id);

            string? description;
            if (playerInfoResponse.IsSuccess)
            {
                Embed fullstatsEmbed = EmbedHelper.FullStats(playerInfoResponse.Value, null, null);
                description = fullstatsEmbed.Description;
            }
            else
            {
                description = playerInfoResponse.ErrorMsg;
            }

            registerEmbed = EmbedHelper.RegisterUserModEmbed(message.Author.Mention, id, description);

            // Keep in sync with HandleRegistration in ComponentInteractions
            string buttonId = $"register:{message.Author.Id}:{id}:{message.Id}";

            cb.WithButton("Register", buttonId, ButtonStyle.Success);
        }
        // User specified "no score"
        else
        {
            registerEmbed = EmbedHelper.GiveUserRoleModEmbed(message.Author.Mention, _config.NoScoreRoleId);

            // The no-score request has no leaderboard ID, so the placeholder NoScoreLeaderboardId
            // is used. It must be a non-negative number so the button ID parses back into a uint.
            string buttonId = $"register:{message.Author.Id}:{NoScoreLeaderboardId}:{message.Id}";

            cb.WithButton("Confirm", buttonId, ButtonStyle.Success);
        }

        cb.WithButton("Deny", $"deny:{message.Author.Id}", ButtonStyle.Danger);

        SocketTextChannel modsChannel = _discordHelper.GetTextChannel(_config.ModsChannelId);
        await modsChannel.SendMessageAsync(embed: registerEmbed, components: cb.Build());

        const string notifMessage = "ℹ️ I've notified the mods. You'll be registered soon.";
        await message.ReplyAsync(embed: new EmbedBuilder().WithDescription(notifMessage).Build(), allowedMentions: AllowedMentions.None);
    }

    private async Task<Result<EntryResponse>> GetEntryResponse(uint lbId)
    {
        try
        {
            IReadOnlyList<EntryResponse> entries = await _webService.GetLbPlayers([lbId]);
            return Result.Success(entries[0]);
        }
        catch (Exception ex)
        {
            string errorMsg = ex switch
            {
                ClubberException => ex.Message,
                HttpRequestException => "Couldn't fetch player data. Either the provided run ID doesn't exist or ddinfo servers are down.",
                SerializationException => "Couldn't read ddinfo player data.",
                _ => "Internal error.",
            };

            return Result.Failure<EntryResponse>(errorMsg)!;
        }
    }
}
