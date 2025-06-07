using Clubber.Discord.Helpers;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;
using System.Runtime.Serialization;

namespace Clubber.Discord.Modules;

[DefaultMemberPermissions(GuildPermission.ManageRoles)]
public sealed class ModeratorCommands(
	IOptions<AppConfig> config,
	IDiscordHelper discordHelper,
	IDatabaseHelper databaseHelper,
	IWebService webService) : InteractionModuleBase<SocketInteractionContext>
{
	private readonly AppConfig _config = config.Value;

	[SlashCommand("edit-news", "Edit a DD news post made by the bot")]
	public async Task EditNewsPost(
		[Summary("message-id", "ID of the message to edit (leave empty for latest)")]
		string? messageId = null,
		[Summary("content", "New content for the message")]
		string newContent = "")
	{
		if (string.IsNullOrWhiteSpace(newContent))
		{
			await RespondAsync("Message content cannot be empty.", ephemeral: true);
			return;
		}

		try
		{
			SocketTextChannel ddnewsPostChannel = discordHelper.GetTextChannel(_config.DdNewsChannelId);
			IUserMessage? messageToEdit = null;

			if (!string.IsNullOrEmpty(messageId) && ulong.TryParse(messageId, out ulong parsedMessageId))
			{
				if (await ddnewsPostChannel.GetMessageAsync(parsedMessageId) is IUserMessage specificMessage)
				{
					messageToEdit = specificMessage;
				}
			}
			else
			{
				IEnumerable<IMessage> messages = await ddnewsPostChannel.GetMessagesAsync(5).FlattenAsync();
				messageToEdit = messages.Where(m => m.Author.Id == Context.Client.CurrentUser.Id)
					.MaxBy(m => m.CreatedAt) as IUserMessage;
			}

			if (messageToEdit == null)
			{
				await RespondAsync("Could not find message.", ephemeral: true);
				return;
			}

			if (messageToEdit.Author.Id != Context.Client.CurrentUser.Id)
			{
				await RespondAsync("That message wasn't posted by the bot.", ephemeral: true);
				return;
			}

			await messageToEdit.ModifyAsync(m => m.Content = newContent);
			await RespondAsync("✅ Message updated successfully!");
		}
		catch (Exception ex)
		{
			await RespondAsync("Failed to edit message.", ephemeral: true);
			Log.Error(ex, "Error editing news post");
		}
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
			await channel.SendMessageAsync(embeds: EmbedHelper.RegisterEmbeds());
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
		[Summary("url-or-id", "ddstats URL or run ID")]
		string urlOrId,
		[Summary("split-name", "Specific split to check (e.g. 350, 700, etc.)")]
		int? splitName = null,
		[Summary("description", "Description for the run")]
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

				response = await databaseHelper.UpdateBestSplitsIfNeeded([split], ddstatsRun, desc);
			}
			else
			{
				response = await databaseHelper.UpdateBestSplitsIfNeeded(splits, ddstatsRun, desc);
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
		[Summary("url-or-id", "ddstats URL or run ID")]
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

			(HomingPeakRun? OldRun, HomingPeakRun? NewRun) response = await databaseHelper.UpdateTopHomingPeaksIfNeeded(possibleNewTopPeakRun);
			if (response.NewRun is null)
			{
				await FollowupAsync("No updates were needed.");
				return;
			}

			string userName = ddStatsRun.GameInfo.PlayerName;
			string? avatarUrl = null;
			DdUser? ddUser = await databaseHelper.FindRegisteredUser(ddStatsRun.GameInfo.PlayerId);
			if (ddUser != null && Context.Guild.GetUser(ddUser.DiscordId) is { } user)
			{
				userName = user.AvailableNameSanitized();
				avatarUrl = user.GetDisplayAvatarUrl() ?? user.GetDefaultAvatarUrl();
			}

			Embed updatedPeakEmbed = EmbedHelper.UpdateTopPeakRuns(userName, response.NewRun, response.OldRun, avatarUrl);
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
}
