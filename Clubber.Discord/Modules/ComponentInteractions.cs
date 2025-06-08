using Clubber.Discord.Helpers;
using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;

namespace Clubber.Discord.Modules;

public sealed class ComponentInteractions(
	IOptions<AppConfig> config,
	UserService userService,
	IDatabaseHelper databaseHelper,
	IDiscordHelper discordHelper,
	RegistrationTracker registrationTracker,
	IWebService webService) : InteractionModuleBase<SocketInteractionContext>
{
	private readonly AppConfig _config = config.Value;

	[ComponentInteraction("register:*:*:*")]
	public async Task HandleRegistration(ulong userId, int leaderboardId, ulong registerMessageId)
	{
		if (Context.Guild?.Id is null)
		{
			await RespondAsync(embeds: [new EmbedBuilder().WithDescription("❌ Internal error.").Build()]);
			await ClearComponents();
			return;
		}

		SocketGuildUser? guildUser = discordHelper.GetGuildUser(Context.Guild.Id, userId);
		if (guildUser is null)
		{
			await RespondAsync(embeds: [new EmbedBuilder().WithDescription("⚠️ Couldn't find the user. They likely left the server.").Build()]);
			await ClearComponents();
			return;
		}

		Result registrationResult = await CheckUserAndRegister(leaderboardId, guildUser);
		if (registrationResult.IsFailure)
		{
			await RespondAsync(embeds: [new EmbedBuilder().WithDescription($"{registrationResult.ErrorMsg}").Build()]);
			await ClearComponents();
			return;
		}

		registrationTracker.UnflagUser(guildUser.Id);

		SocketTextChannel? registerChannel = null;
		try
		{
			registerChannel = discordHelper.GetTextChannel(_config.RegisterChannelId);
		}
		catch (Exception e)
		{
			Log.Warning(e, "Failed to retrieve Register channel");
		}

		string modsSuccessEmbedDescription = "✅ Done!";
		if (registerChannel == null)
		{
			modsSuccessEmbedDescription += "\n\n⚠️ Register channel couldn't be found so the user couldn't be informed of their registration.";
		}
		else
		{
			string userSuccessMsg = leaderboardId > 0
				? "✅ You've been registered!\n\nPlease do `+pb` anywhere to get the role."
				: "✅ Done!\n\nℹ️ Keep in mind you'll have limited channel access, but you can always ping the mods to get registered.";

			Embed userSuccessEmbed = new EmbedBuilder().WithDescription(userSuccessMsg).Build();
			if (await registerChannel.GetMessageAsync(registerMessageId) is IUserMessage userMsg)
			{
				await userMsg.ReplyAsync(embeds: [userSuccessEmbed]);
			}
			else
			{
				await registerChannel.SendMessageAsync(guildUser.Mention, embeds: [userSuccessEmbed]);
			}
		}

		Embed modsSuccessEmbed = new EmbedBuilder()
			.WithDescription(modsSuccessEmbedDescription)
			.Build();

		await ClearComponents();
		await RespondAsync(embeds: [modsSuccessEmbed]);
	}

	[ComponentInteraction("deny_button")]
	public async Task HandleDeny()
	{
		await RespondAsync(embeds: [new EmbedBuilder().WithDescription("ℹ️ Interaction closed.").Build()]);
		await ClearComponents();
	}

	[ComponentInteraction("stats:*:*")]
	public async Task HandleStats(ulong userId, uint leaderboardId)
	{
		if (Context.Guild?.Id is null)
		{
			await RespondAsync(embeds: [new EmbedBuilder().WithDescription("❌ Internal error.").Build()]);
			await ClearComponents();
			return;
		}

		await DeferAsync();

		try
		{
			Task<IReadOnlyList<EntryResponse>> playerEntryTask = webService.GetLbPlayers([leaderboardId]);
			Task<GetPlayerHistory?> playerHistoryTask = webService.GetPlayerHistory(leaderboardId);
			await Task.WhenAll(playerEntryTask, playerHistoryTask);

			EntryResponse playerEntry = (await playerEntryTask)[0];
			GetPlayerHistory? playerHistory = await playerHistoryTask;

			SocketGuildUser? user = discordHelper.GetGuildUser(Context.Guild.Id, userId);

			Embed fullStatsEmbed = EmbedHelper.FullStats(playerEntry, user, playerHistory);
			await ClearComponents();
			await Context.Interaction.ModifyOriginalResponseAsync(m => m.Embeds = new Optional<Embed[]>([fullStatsEmbed]));
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error handling stats button interaction");
			await FollowupAsync("Failed to load stats.", ephemeral: true);
		}
	}

	private async Task<Result> CheckUserAndRegister(int lbId, SocketGuildUser user)
	{
		Result result = await userService.IsValidForRegistration(user, false);
		if (result.IsFailure)
		{
			return Result.Failure($"⚠️ {result.ErrorMsg}");
		}

		if (lbId < 0)
		{
			await user.AddRoleAsync(_config.NoScoreRoleId);
		}
		else
		{
			Result registrationResult = await databaseHelper.RegisterUser((uint)lbId, user.Id);
			if (registrationResult.IsFailure)
			{
				return Result.Failure($"❌ Failed to execute command: {registrationResult.ErrorMsg}");
			}

			const ulong newPalRoleId = 728663492424499200;
			const ulong pendingPbRoleId = 994354086646399066;
			await user.RemoveRoleAsync(newPalRoleId);
			await user.AddRoleAsync(pendingPbRoleId);
		}

		return Result.Success();
	}

	private async Task ClearComponents()
	{
		if (Context.Interaction is SocketMessageComponent component)
		{
			await component.ClearMessageComponents();
		}
	}
}
