using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace Clubber.Discord.Modules;

[Group("checkhoming")]
[Alias("checkpeak")]
[Summary("Checks if the provided ddstats run has better splits than the current best ones and updates if necessary.")]
[RequireAdminOrRole(697777821954736179, ErrorMessage = "Only moderators can use this command.")]
[RequireContext(ContextType.Guild)]
public class PeakhomingModule : ExtendedModulebase<SocketCommandContext>
{
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IWebService _webService;
	private const int _homingPeakLimit = 1500;

	public PeakhomingModule(IDatabaseHelper databaseHelper, IWebService webService)
	{
		_databaseHelper = databaseHelper;
		_webService = webService;
	}

	[Priority(2)]
	[Command]
	[Remarks("checkhoming 123456789")]
	public async Task FromRunId(uint runId)
		=> await FromDdstatsUrl($"https://ddstats.com/api/v2/game/full?id={runId}");

	[Priority(1)]
	[Command]
	[Remarks("checkhoming https://ddstats.com/games/123456789")]
	public async Task FromDdstatsUrl(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
		{
			await InlineReplyAsync("Invalid URL.");
			return;
		}

		if (await GetDdstatsResponse(uri) is not { } ddStatsRun)
			return;

		if (await IsError(!ddStatsRun.GameInfo.Spawnset.Equals("v3", StringComparison.InvariantCultureIgnoreCase), "It has to be a v3 run."))
			return;

		if (await IsError(ddStatsRun.GameInfo.HomingDaggersMax > _homingPeakLimit, $"Invalid run: the homing peak is unrealistically high (>{_homingPeakLimit})."))
			return;

		HomingPeakRun possibleNewTopPeakRun = new()
		{
			PlayerName = ddStatsRun.GameInfo.PlayerName,
			PlayerLeaderboardId = ddStatsRun.GameInfo.PlayerId,
			HomingPeak = ddStatsRun.GameInfo.HomingDaggersMax,
			Source = $"https://ddstats.com/games/{ddStatsRun.GameInfo.Id}",
		};

		(HomingPeakRun? OldRun, HomingPeakRun? NewRun) response = await _databaseHelper.UpdateTopHomingPeaksIfNeeded(possibleNewTopPeakRun);
		if (response.NewRun is null)
		{
			await InlineReplyAsync("No updates were needed.");
			return;
		}

		string userName = ddStatsRun.GameInfo.PlayerName;
		string? avatarUrl = null;
		DdUser? ding = await _databaseHelper.GetDdUserBy(ddStatsRun.GameInfo.PlayerId);
		if (ding != null && Context.Guild.GetUser(ding.DiscordId) is { } user)
		{
			userName = user.AvailableNameSanitized();
			avatarUrl = user.GetDisplayAvatarUrl() ?? user.GetDefaultAvatarUrl();
		}

		Embed updatedRolesEmbed = EmbedHelper.UpdateTopPeakRuns(userName, response.NewRun, response.OldRun, avatarUrl);
		await ReplyAsync(embed: updatedRolesEmbed, allowedMentions: AllowedMentions.None, messageReference: new(Context.Message.Id));
	}

	private async Task<DdStatsFullRunResponse?> GetDdstatsResponse(Uri uri)
	{
		try
		{
			return await _webService.GetDdstatsResponse(uri);
		}
		catch (Exception ex)
		{
			string errorMsg = ex switch
			{
				ClubberException           => ex.Message,
				HttpRequestException       => "Couldn't fetch run data. Either the provided run ID doesn't exist or ddstats servers are down.",
				JsonSerializationException => "Couldn't read ddstats run data.",
				_                          => "Internal error.",
			};

			await InlineReplyAsync($"Failed to run command: {errorMsg}");
			return null;
		}
	}
}
