using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Web.Server.Endpoints;

public static class ClubberEndpoints
{
	public static void RegisterClubberEndpoints(this WebApplication app)
	{
		app.MapGet("/", RootPage);

		app.MapGet("/users", RegisteredUsers).WithTags("Users");

		app.MapGet("/users/by-leaderboardId", UserByLeaderboardId).WithTags("Users");

		app.MapGet("/users/by-discordId", UserByDiscordId).WithTags("Users");

		app.MapGet("/dailynews", DailyNews).WithTags("News");

		app.MapGet("/bestsplits", BestSplits).WithTags("Splits");

		app.MapGet("/bestsplits/by-splitname", BestSplitByName).WithTags("Splits");
	}

	private static async Task<BestSplit?> BestSplitByName(string splitName, IServiceScopeFactory scopeFactory)
	{
		using IServiceScope scope = scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.BestSplits.AsNoTracking().FirstOrDefaultAsync(bs => bs.Name == splitName);
	}

	private static async Task<BestSplit[]> BestSplits(IServiceScopeFactory scopeFactory)
	{
		using IServiceScope scope = scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.BestSplits.AsNoTracking().ToArrayAsync();
	}

	private static async Task<List<DdNewsItem>> DailyNews(IServiceScopeFactory scopeFactory)
	{
		using IServiceScope scope = scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdNews.AsNoTracking().ToListAsync();
	}

	private static async Task<DdUser?> UserByDiscordId(ulong discordId, IServiceScopeFactory scopeFactory)
	{
		using IServiceScope scope = scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(user => user.DiscordId == discordId);
	}

	private static async Task<DdUser?> UserByLeaderboardId(int leaderboardId, IServiceScopeFactory scopeFactory)
	{
		using IServiceScope scope = scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(user => user.LeaderboardId == leaderboardId);
	}

	private static async Task<List<DdUser>> RegisteredUsers(IDatabaseHelper dbhelper)
	{
		return await dbhelper.GetEntireDatabase();
	}

	private static async Task RootPage(HttpContext context)
	{
		string indexHtmlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Pages", "Index.html");
		string indexHtml = await File.ReadAllTextAsync(indexHtmlPath);
		await context.Response.WriteAsync(indexHtml);
	}
}
