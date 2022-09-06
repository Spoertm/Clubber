using Clubber.Helpers;
using Clubber.Models;
using Clubber.Models.DdSplits;
using Clubber.Models.Responses;
using Clubber.Preconditions;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace Clubber.Modules;

[Group("checkhoming")]
[Alias("checkpeak")]
[Summary("Checks if the provided ddstats run has better splits than the current best ones and updates if necessary.")]
[RequireRole(697777821954736179, ErrorMessage = "Only moderators can use this command.")]
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
		if (await GetDdstatsResponse(url) is not { } ddStatsRun)
			return;

		if (await IsError(!ddStatsRun.GameInfo.Spawnset.Equals("v3", StringComparison.InvariantCultureIgnoreCase), "That's not a V3 run."))
			return;

		int? homingPeak = RunAnalyzer.HomingPeak(ddStatsRun);
		if (await IsError(homingPeak is null, "Failed to find homing peak. "))
			return;

		if (await IsError(homingPeak > _homingPeakLimit, $"Invalid run: the homing peak is unrealistically high (>{_homingPeakLimit})."))
			return;

		HomingPeakRun possibleNewTopPeakRun = new()
		{
			PlayerName = ddStatsRun.GameInfo.PlayerName,
			PlayerLeaderboardId = ddStatsRun.GameInfo.PlayerId,
			HomingPeak = homingPeak!.Value,
			Source = $"https://ddstats.com/games/{ddStatsRun.GameInfo.Id}",
		};

		(HomingPeakRun[] OldTopPeaks, HomingPeakRun? NewPeakRun) response = await _databaseHelper.UpdateTopHomingPeaksIfNeeded(possibleNewTopPeakRun);
		if (response.NewPeakRun is null)
		{
			await InlineReplyAsync("No updates were needed.");
			return;
		}

		Embed updatedRolesEmbed = EmbedHelper.UpdateTopPeakRuns(response.OldTopPeaks, response.NewPeakRun);
		await ReplyAsync(embed: updatedRolesEmbed, allowedMentions: AllowedMentions.None, messageReference: Context.Message.Reference);
	}

	private async Task<DdStatsFullRunResponse?> GetDdstatsResponse(string url)
	{
		try
		{
			return await _webService.GetDdstatsResponse(url);
		}
		catch (Exception ex)
		{
			string errorMsg = ex switch
			{
				CustomException            => ex.Message,
				HttpRequestException       => "Couldn't fetch run data. Either the provided run ID doesn't exist or ddstats servers are down.",
				JsonSerializationException => "Couldn't read ddstats run data.",
				_                          => "Internal error.",
			};

			await InlineReplyAsync($"Failed to run command: {errorMsg}");
			return null;
		}
	}
}
