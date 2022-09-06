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

[Group("checksplits")]
[Summary("Checks if the provided ddstats run has better splits than the current best ones and updates if necessary.")]
[RequireRole(697777821954736179, ErrorMessage = "Only moderators can use this command.")]
[RequireContext(ContextType.Guild)]
public class SplitsModule : ExtendedModulebase<SocketCommandContext>
{
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IWebService _webService;

	public SplitsModule(IDatabaseHelper databaseHelper, IWebService webService)
	{
		_databaseHelper = databaseHelper;
		_webService = webService;
	}

	[Priority(4)]
	[Command]
	[Remarks("checksplits 123456789 350\nchecksplits 123456789 350 SomeDescription.")]
	public async Task FromRunId(uint runId, uint splitname, [Remainder] string? description = null)
		=> await FromDdstatsUrl($"https://ddstats.com/api/v2/game/full?id={runId}", splitname, description);

	[Priority(3)]
	[Command]
	[Remarks("checksplits 123456789\nchecksplits 123456789 SomeDescription.")]
	public async Task FromRunId(uint runId, [Remainder] string? description = null)
		=> await FromDdstatsUrl($"https://ddstats.com/api/v2/game/full?id={runId}", description);

	[Priority(2)]
	[Command]
	[Remarks("checksplits https://ddstats.com/games/123456789 350\nchecksplits https://ddstats.com/games/123456789 350 SomeDescription.")]
	public async Task FromDdstatsUrl(string url, uint splitName, [Remainder] string? description = null)
	{
		string[] v3SplitNames = Split.V3Splits.Select(s => s.Name).ToArray();
		if (await IsError(!v3SplitNames.Contains(splitName.ToString()), $"The split `{splitName}` doesn't exist."))
			return;

		if (await GetDdstatsResponse(url) is not { } ddstatsRun)
			return;

		if (await IsError(!ddstatsRun.GameInfo.Spawnset.Equals("v3", StringComparison.InvariantCultureIgnoreCase), "That's not a V3 run."))
			return;

		Split? split = RunAnalyzer.GetData(ddstatsRun).FirstOrDefault(s => s.Name == splitName.ToString());
		if (await IsError(split is null, $"The split `{splitName}` isn't in the run.") ||
			await IsError(split!.Value > 1000, "Invalid run: too many homings gained on that split."))
			return;

		string desc = description ?? $"{ddstatsRun.GameInfo.PlayerName} {ddstatsRun.GameInfo.GameTime:0.0000}";
		(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits) response = await _databaseHelper.UpdateBestSplitsIfNeeded(new[] { split }, ddstatsRun, desc);
		if (response.UpdatedBestSplits.Length == 0)
		{
			await InlineReplyAsync("No updates were needed.");
			return;
		}

		Embed updatedRolesEmbed = EmbedHelper.UpdatedSplits(response.OldBestSplits, response.UpdatedBestSplits);
		await ReplyAsync(embed: updatedRolesEmbed, allowedMentions: AllowedMentions.None, messageReference: Context.Message.Reference);
	}

	[Priority(1)]
	[Command]
	[Remarks("checksplits https://ddstats.com/games/123456789\nchecksplits https://ddstats.com/games/123456789 SomeDescription")]
	public async Task FromDdstatsUrl(string url, [Remainder] string? description = null)
	{
		if (await GetDdstatsResponse(url) is not { } ddStatsRun)
			return;

		if (await IsError(!ddStatsRun.GameInfo.Spawnset.Equals("v3", StringComparison.InvariantCultureIgnoreCase), "That's not a V3 run."))
			return;

		Split[] splits = RunAnalyzer.GetData(ddStatsRun);
		if (await IsError(splits.Any(s => s.Value > 1000), "Invalid run: too many homings gained on some splits."))
			return;

		string desc = description ?? $"{ddStatsRun.GameInfo.PlayerName} {ddStatsRun.GameInfo.GameTime:0.0000}";
		(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits) response = await _databaseHelper.UpdateBestSplitsIfNeeded(splits, ddStatsRun, desc);
		if (response.UpdatedBestSplits.Length == 0)
		{
			await InlineReplyAsync("No updates were needed.");
			return;
		}

		Embed updatedRolesEmbed = EmbedHelper.UpdatedSplits(response.OldBestSplits, response.UpdatedBestSplits);
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
