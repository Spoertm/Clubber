using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Web.Endpoints;

internal static class ClubberEndpoints
{
	public static void RegisterClubberEndpoints(this WebApplication app)
	{
		app.MapGet("/users", RegisteredUsers).WithTags("Users");

		app.MapGet("/users/by-leaderboardId", UserByLeaderboardId).WithTags("Users");

		app.MapGet("/users/by-discordId", UserByDiscordId).WithTags("Users");

		app.MapGet("/dailynews", DailyNews).WithTags("News");

		app.MapGet("/bestsplits", BestSplits).WithTags("Splits");

		app.MapGet("/bestsplits/by-splitname", BestSplitByName).WithTags("Splits");
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

	private static async Task<List<DdNewsItem>> DailyNews(IServiceScopeFactory scopeFactory)
	{
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdNews.AsNoTracking().ToListAsync();
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
}
