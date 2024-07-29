using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Clubber.Domain.Extensions;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System.Runtime.Serialization;

namespace Clubber.Discord.Services;

public class RegistrationRequestHandler
{
	private readonly AppConfig _config;
	private readonly RegistrationTracker _registrationTracker;
	private readonly IWebService _webService;
	private readonly IDiscordHelper _discordHelper;
	private readonly IDatabaseHelper _databaseHelper;

	public RegistrationRequestHandler(
		IOptions<AppConfig> config,
		RegistrationTracker registrationTracker,
		IWebService webService,
		IDiscordHelper discordHelper,
		ClubberDiscordClient clubberDiscordClient,
		IDatabaseHelper databaseHelper)
	{
		_config = config.Value;
		_registrationTracker = registrationTracker;
		_webService = webService;
		_discordHelper = discordHelper;
		_databaseHelper = databaseHelper;

		clubberDiscordClient.Ready += () =>
		{
			clubberDiscordClient.MessageReceived += message => Task.Run(() => Handle(message));
			return Task.CompletedTask;
		};
	}

	private async Task Handle(SocketMessage socketMessage)
	{
		if (!(socketMessage is SocketUserMessage { Source: MessageSource.User } message && message.Channel.Id == _config.RegisterChannelId))
		{
			return;
		}

		if (await _databaseHelper.FindRegisteredUser(message.Author.Id) != null)
		{
			return;
		}

		if (_registrationTracker.UserIsFlagged(message.Author.Id))
		{
			await message.ReplyAsync(embed: new EmbedBuilder().WithDescription("ℹ️ You've already provided an ID. Mods will register you soon.").Build());
			return;
		}

		Embed? registerEmbed;
		ComponentBuilder cb = new();

		// User specified an ID
		if (message.Content.FindFirstInt() is var foundId and > 0)
		{
			Result<EntryResponse> playerInfoResponse = await GetEntryResponse((uint)foundId);

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

			registerEmbed = EmbedHelper.RegisterUserModEmbed(message.Author.Mention, foundId, description);

			// Refer to RegistrationContext.Parse in InteractionHandler
			string buttonId = $"register:{message.Author.Id}:{foundId}:{message.Id}";

			cb.WithButton("Register", buttonId, ButtonStyle.Success);
			cb.WithButton("Deny", "deny_button", ButtonStyle.Danger);
		}
		// User specified "no score"
		else if (message.Content.Contains("no score", StringComparison.OrdinalIgnoreCase))
		{
			registerEmbed = EmbedHelper.GiveUserRoleModEmbed(message.Author.Mention, _config.NoScoreRoleId);

			string buttonId = $"register:{message.Author.Id}:-1:{message.Id}";

			cb.WithButton("Confirm", buttonId, ButtonStyle.Success);
			cb.WithButton("Deny", "deny_button", ButtonStyle.Danger);
		}
		else
		{
			return;
		}

		_registrationTracker.FlagUser(message.Author.Id);

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
				ClubberException       => ex.Message,
				HttpRequestException   => "Couldn't fetch player data. Either the provided run ID doesn't exist or ddinfo servers are down.",
				SerializationException => "Couldn't read ddinfo player data.",
				_                      => "Internal error.",
			};

			return Result.Failure<EntryResponse>(errorMsg)!;
		}
	}
}
