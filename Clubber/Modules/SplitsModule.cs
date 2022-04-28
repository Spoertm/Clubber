using Clubber.Helpers;
using Clubber.Models.DdSplits;
using Clubber.Models.Responses;
using Clubber.Preconditions;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace Clubber.Modules;

[Group("checksplits")]
[Summary("Checks if the provided ddstats run has better splits than the current best ones and updates if necessary.")]
[RequireRole(552525894321700864)]
[RequireRole(701868700365488281)]
public class SplitsModule : ExtendedModulebase<SocketCommandContext>
{
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IHttpClientFactory _httpClientFactory;

	public SplitsModule(IDatabaseHelper databaseHelper, IHttpClientFactory httpClientFactory)
	{
		_databaseHelper = databaseHelper;
		_httpClientFactory = httpClientFactory;
	}

	[Priority(4)]
	[Command]
	[Remarks("checksplits 123456789 350\nchecksplits 123456789 350 SomeDescription.")]
	public async Task FromRunId(uint runId, string splitname, [Remainder] string? description = null)
		=> await FromDdstatsUrl($"https://ddstats.com/api/v2/game/full?id={runId}", splitname, description);

	[Priority(3)]
	[Command]
	[Remarks("checksplits 123456789\nchecksplits 123456789 SomeDescription.")]
	public async Task FromRunId(uint runId, [Remainder] string? description = null)
		=> await FromDdstatsUrl($"https://ddstats.com/api/v2/game/full?id={runId}", description);

	[Priority(2)]
	[Command]
	[Remarks("checksplits https://ddstats.com/games/123456789 350\nchecksplits https://ddstats.com/games/123456789 350 SomeDescription.")]
	public async Task FromDdstatsUrl(string url, string splitName, [Remainder] string? description = null)
	{
		string[] v3SplitNames = Split.V3Splits.Select(s => s.Name).ToArray();
		if (await IsError(!v3SplitNames.Contains(splitName), "That split doesn't exist."))
			return;

		if (await GetDdstatsResponse(url) is not { } ddStatsRun)
			return;

		if (await IsError(!ddStatsRun.GameInfo.Spawnset.Equals("v3", StringComparison.InvariantCultureIgnoreCase), "That's not a V3 run."))
			return;

		Split? split = RunAnalyzer.GetData(ddStatsRun).FirstOrDefault(s => s.Name == splitName);
		if (await IsError(split is null, "That split isn't in the run.") ||
			await IsError(split!.Value > 1000, "Invalid run: too many homings gained on that split."))
			return;

		string desc = description ?? $"{ddStatsRun.GameInfo.PlayerName} {ddStatsRun.GameInfo.GameTime:0.0000}";
		(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits) response = await _databaseHelper.UpdateBestSplitsIfNeeded(new[] { split }, ddStatsRun, desc);
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
		if (await IsError(!Uri.IsWellFormedUriString(url, UriKind.Absolute), "Invalid URL."))
			return null;

		string runIdStr = string.Empty;
		if (url.StartsWith("https://ddstats.com/games/"))
			runIdStr = url[26..];
		else if (url.StartsWith("https://www.ddstats.com/games/"))
			runIdStr = url[30..];
		else if (url.StartsWith("https://ddstats.com/api/v2/game/full"))
			runIdStr = url[40..];

		bool successfulParse = uint.TryParse(runIdStr, out uint runId);
		if (await IsError(string.IsNullOrEmpty(runIdStr), "") ||
			await IsError(!successfulParse, "Invalid ddstats URL."))
			return null;

		try
		{
			string fullRunReqUrl = $"https://ddstats.com/api/v2/game/full?id={runId}";
			string ddstatsResponse = await _httpClientFactory.CreateClient().GetStringAsync(fullRunReqUrl);
			return JsonConvert.DeserializeObject<DdStatsFullRunResponse>(ddstatsResponse) ?? throw new JsonSerializationException();
		}
		catch (Exception ex)
		{
			string errorMsg = ex switch
			{
				HttpRequestException       => "Couldn't fetch run data. Either the provided run ID doesn't exist or ddstats servers are down.",
				JsonSerializationException => "Couldn't read ddstats run data.",
				_                          => "Internal error.",
			};

			await InlineReplyAsync($"Failed to run command: {errorMsg}");
			return null;
		}
	}
}
