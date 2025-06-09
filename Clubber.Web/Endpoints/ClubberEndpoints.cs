using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Services;
using Clubber.Web.Models;
using Microsoft.EntityFrameworkCore;

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

	private static async Task<BestSplit?> BestSplitByName(string splitName, IServiceScopeFactory scopeFactory)
	{
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.BestSplits.FindAsync(splitName);
	}

	private static async Task<List<BestSplit>> BestSplits(IServiceScopeFactory scopeFactory)
	{
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.BestSplits.AsNoTracking().ToListAsync();
	}

	private static async Task<List<DdNewsItemDto>> DailyNews(IServiceScopeFactory scopeFactory)
	{
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdNews.AsNoTracking()
			.Select(item => new DdNewsItemDto(item.LeaderboardId,
				item.OldEntry,
				item.NewEntry,
				item.TimeOfOccurenceUtc,
				item.Nth))
			.ToListAsync();
	}

	private static async Task<DdUser?> UserByDiscordId(ulong discordId, IServiceScopeFactory scopeFactory)
	{
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(user => user.DiscordId == discordId);
	}

	private static async Task<DdUser?> UserByLeaderboardId(int leaderboardId, IServiceScopeFactory scopeFactory)
	{
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdPlayers.FindAsync(leaderboardId);
	}

	private static async Task<List<DdUser>> RegisteredUsers(IDatabaseHelper dbHelper)
	{
		return await dbHelper.GetRegisteredUsers();
	}

	private static async Task<int> RegisteredUserCount(IServiceScopeFactory scopeFactory)
	{
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdPlayers.CountAsync();
	}
}
