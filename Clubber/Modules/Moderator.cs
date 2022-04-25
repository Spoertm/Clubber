using Clubber.Helpers;
using Clubber.Models.DdSplits;
using Clubber.Models.Responses;
using Clubber.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Clubber.Modules;

[Name("Moderator")]
[RequireRole(697777821954736179, ErrorMessage = "Only moderators can use this command.")]
[RequireContext(ContextType.Guild)]
public class Moderator : ExtendedModulebase<SocketCommandContext>
{
	private readonly IConfiguration _config;
	private readonly IDiscordHelper _discordHelper;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IDatabaseHelper _databaseHelper;

	public Moderator(
		IConfiguration config,
		IDiscordHelper discordHelper,
		IHttpClientFactory httpClientFactory,
		IDatabaseHelper databaseHelper)
	{
		_config = config;
		_discordHelper = discordHelper;
		_httpClientFactory = httpClientFactory;
		_databaseHelper = databaseHelper;
	}

	[Command("editnewspost")]
	[Summary("Allows you to edit a DD news post made by the bot. If no message ID is given, then the latest post will be edited.")]
	[Remarks("editnewspost 123456789 This is the new text!")]
	[Priority(1)]
	public async Task EditDdNewsPost([Name("message ID")] ulong messageId, [Name("text")][Remainder] string newMessage)
	{
		if (await IsError(string.IsNullOrWhiteSpace(newMessage), "Message can't be empty."))
			return;

		SocketTextChannel ddnewsPostChannel = _discordHelper.GetTextChannel(_config.GetValue<ulong>("DdNewsChannelId"));
		if (await ddnewsPostChannel.GetMessageAsync(messageId) is not IUserMessage messageToEdit)
		{
			await InlineReplyAsync("Could not find message.");
			return;
		}

		if (await IsError(messageToEdit.Author.Id != Context.Client.CurrentUser.Id, "That message wasn't posted by the bot."))
			return;

		await messageToEdit.ModifyAsync(m => m.Content = newMessage);
		await ReplyAsync("✅ Done!");
	}

	[Command("editnewspost")]
	[Summary("Allows you to edit a DD news post made by the bot. If no message ID is given, then the latest post will be edited.")]
	[Remarks("editnewspost This is the new text!")]
	[Priority(0)]
	public async Task EditDdNewsPost([Name("text")][Remainder] string newMessage)
	{
		if (await IsError(string.IsNullOrWhiteSpace(newMessage), "Message can't be empty."))
			return;

		SocketTextChannel ddnewsPostChannel = _discordHelper.GetTextChannel(_config.GetValue<ulong>("DdNewsChannelId"));
		IEnumerable<IMessage> messages = await ddnewsPostChannel.GetMessagesAsync(5).FlattenAsync();
		if (messages.Where(m => m.Author.Id == Context.Client.CurrentUser.Id).MaxBy(m => m.CreatedAt) is not IUserMessage messageToEdit)
		{
			await InlineReplyAsync("Could not find message.");
			return;
		}

		await messageToEdit.ModifyAsync(m => m.Content = newMessage);
		await ReplyAsync("✅ Done!");
	}

	[Command("clear")]
	[Summary("Clears all messages but the first one in #register channel.")]
	[Remarks("clear")]
	public async Task Clear()
	{
		ulong registerChannelId = _config.GetValue<ulong>("RegisterChannelId");
		if (Context.Channel is not SocketTextChannel currentTextChannel || currentTextChannel.Id != registerChannelId)
		{
			await ReplyAsync($"This command can only be run in <#{registerChannelId}>.");
			return;
		}

		IEnumerable<IMessage> lastHundredMessages = await currentTextChannel.GetMessagesAsync().FlattenAsync();
		IEnumerable<IMessage> messagesToDelete = lastHundredMessages
			.OrderByDescending(m => m.CreatedAt)
			.SkipLast(1);

		await currentTextChannel.DeleteMessagesAsync(messagesToDelete);
	}

	[Priority(1)]
	[Command("checksplits")]
	[Summary("Checks if the provided ddstats run has better splits than the current best ones and updates if necessary.")]
	[Remarks("checksplits https://ddstats.com/games/123456789\nchecksplits https://ddstats.com/games/123456789 This is a description.")]
	[RequireRole(552525894321700864)]
	[RequireRole(701868700365488281)]
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

	[Priority(2)]
	[Command("checksplits")]
	[Summary("Checks if the provided ddstats run has a better split than the current best one and updates if necessary.")]
	[Remarks(@"checksplits https://ddstats.com/games/123456789
checksplits https://ddstats.com/games/123456789 350
checksplits https://ddstats.com/games/123456789 350 description.")]
	[RequireRole(552525894321700864)]
	[RequireRole(701868700365488281)]
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
			await IsError(split.Value > 1000, "Invalid run: too many homings gained on that split."))
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

		DdStatsFullRunResponse ddStatsRun;
		try
		{
			string fullRunReqUrl = $"https://ddstats.com/api/v2/game/full?id={runId}";
			string ddstatsResponse = await _httpClientFactory.CreateClient().GetStringAsync(fullRunReqUrl);
			ddStatsRun = JsonConvert.DeserializeObject<DdStatsFullRunResponse>(ddstatsResponse) ?? throw new JsonSerializationException();
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

		return ddStatsRun;
	}
}
