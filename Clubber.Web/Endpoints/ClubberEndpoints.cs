using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Repositories;
using Clubber.Domain.Models.DdSplits;
using Clubber.Web.Models;

namespace Clubber.Web.Endpoints;

internal static class ClubberEndpoints
{
	public static void RegisterClubberEndpoints(this WebApplication app)
	{
		app.MapGet("/users", RegisteredUsers)
			.WithTags("Users")
			.WithSummary("Get all registered users")
			.WithDescription(
				"Returns a list of all users registered with the bot, including their Discord ID, leaderboard ID, and optional Twitch username.")
			.Produces<List<DdUser>>();

		app.MapGet("/user-count", RegisteredUserCount)
			.WithTags("Users")
			.WithSummary("Get registered user count")
			.WithDescription("Returns the total number of users registered with the bot.")
			.Produces<int>();

		app.MapGet("/users/by-leaderboardId", UserByLeaderboardId)
			.WithTags("Users")
			.WithSummary("Get user by leaderboard ID")
			.WithDescription("Find a registered user by their Devil Daggers leaderboard ID.")
			.Produces<DdUser?>();

		app.MapGet("/users/by-discordId", UserByDiscordId)
			.WithTags("Users")
			.WithSummary("Get user by Discord ID")
			.WithDescription("Find a registered user by their Discord user ID.")
			.Produces<DdUser?>();

		app.MapGet("/dailynews", DailyNews)
			.WithTags("News")
			.WithSummary("Get recent DD news")
			.WithDescription(
				"Returns recent Devil Daggers news items for scores above 1000s. News items are automatically cleaned up after 24 hours.")
			.Produces<List<DdNewsItemDto>>();

		app.MapGet("/bestsplits", BestSplits)
			.WithTags("Splits")
			.WithSummary("Get all best splits")
			.WithDescription(
				"Returns the current best Devil Daggers splits from community runs, including homing dagger counts for each time milestone.")
			.Produces<List<BestSplit>>();

		string validSplitNamesCsv = string.Join(", ", Split.V3Splits.Select(s => s.Name));
		app.MapGet("/bestsplits/by-splitname", BestSplitByName)
			.WithTags("Splits")
			.WithSummary("Get best split by name")
			.WithDescription($"Get the best split record for a specific time milestone. Valid split names: {validSplitNamesCsv}.")
			.Produces<BestSplit?>();
	}

	private static async Task<BestSplit?> BestSplitByName(string splitName, ILeaderboardRepository leaderboardRepository)
	{
		BestSplit[] splits = await leaderboardRepository.GetBestSplitsAsync();
		return splits.FirstOrDefault(s => s.Name == splitName);
	}

	private static async Task<BestSplit[]> BestSplits(ILeaderboardRepository leaderboardRepository)
	{
		return await leaderboardRepository.GetBestSplitsAsync();
	}

	private static async Task<DdNewsItemDto[]> DailyNews(INewsRepository newsRepository)
	{
		DdNewsItem[] items = await newsRepository.GetRecentAsync();
		return [.. items.Select(item => new DdNewsItemDto(item.LeaderboardId,
			item.OldEntry,
			item.NewEntry,
			item.TimeOfOccurenceUtc,
			item.Nth))];
	}

	private static async Task<DdUser?> UserByDiscordId(ulong discordId, IUserRepository userRepository)
	{
		return await userRepository.FindAsync(discordId);
	}

	private static async Task<DdUser?> UserByLeaderboardId(int leaderboardId, IUserRepository userRepository)
	{
		return await userRepository.FindAsync(leaderboardId);
	}

	private static async Task<List<DdUser>> RegisteredUsers(IUserRepository userRepository)
	{
		return await userRepository.GetAllAsync();
	}

	private static async Task<int> RegisteredUserCount(IUserRepository userRepository)
	{
		return await userRepository.GetCountAsync();
	}
}
