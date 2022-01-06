using Clubber.Models;
using Clubber.Models.Responses;
using Clubber.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Helpers;

public class DatabaseHelper : IDatabaseHelper
{
	private readonly IWebService _webService;
	private readonly IServiceScopeFactory _scopeFactory;

	public DatabaseHelper(IWebService webService, IServiceScopeFactory scopeFactory)
	{
		_webService = webService;
		_scopeFactory = scopeFactory;

		using IServiceScope scope = _scopeFactory.CreateScope();
		using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
	}

	public async Task<List<DdUser>> GetEntireDatabase()
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
		return dbContext.DdPlayers.AsNoTracking().ToList();
	}

	public async Task<(bool Success, string Message)> RegisterUser(uint lbId, SocketGuildUser user)
	{
		try
		{
			uint[] playerRequest = { lbId };

			EntryResponse lbPlayer = (await _webService.GetLbPlayers(playerRequest))[0];
			DdUser newDdUser = new(user.Id, lbPlayer.Id);
			using IServiceScope scope = _scopeFactory.CreateScope();
			await using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
			await dbContext.AddAsync(newDdUser);
			await dbContext.SaveChangesAsync();
			return (true, string.Empty);
		}
		catch (Exception ex)
		{
			return ex switch
			{
				CustomException      => (false, ex.Message),
				HttpRequestException => (false, "DD servers are most likely down."),
				IOException          => (false, "IO error."),
				_                    => (false, "No reason specified."),
			};
		}
	}

	public async Task<(bool Success, string Message)> RegisterTwitch(ulong userId, string twitchUsername)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
		if (dbContext.DdPlayers.FirstOrDefault(ddp => ddp.DiscordId == userId) is not { } ddUser)
			return (false, "Couldn't find user in database.");

		ddUser.TwitchUsername = twitchUsername;
		await dbContext.SaveChangesAsync();
		return (true, string.Empty);
	}

	public async Task<bool> RemoveUser(SocketGuildUser user)
		=> await RemoveUser(user.Id);

	public async Task<bool> RemoveUser(ulong discordId)
	{
		DdUser? toRemove = GetDdUserBy(discordId);
		if (toRemove is null)
			return false;

		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
		dbContext.Remove(toRemove);
		await dbContext.SaveChangesAsync();
		return true;
	}

	public DdUser? GetDdUserBy(int lbId)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
		return dbContext.DdPlayers.FirstOrDefault(ddp => ddp.LeaderboardId == lbId);
	}

	public DdUser? GetDdUserBy(ulong discordId)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
		return dbContext.DdPlayers.FirstOrDefault(ddp => ddp.DiscordId == discordId);
	}

	public async Task UpdateLeaderboardCache(List<EntryResponse> newEntries)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
		await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE leaderboard_cache");
		await dbContext.LeaderboardCache.AddRangeAsync(newEntries);
		await dbContext.SaveChangesAsync();
	}

	public async Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();

		DdNewsItem newItem = new(oldEntry.Id, oldEntry, newEntry, DateTime.UtcNow, nth);
		await dbContext.DdNews.AddAsync(newItem);
		await dbContext.SaveChangesAsync();
	}

	public async Task CleanUpNewsItems()
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DatabaseService dbContext = scope.ServiceProvider.GetRequiredService<DatabaseService>();
		DateTime utcNow = DateTime.UtcNow;
		IQueryable<DdNewsItem> toRemove = dbContext.DdNews.Where(ddn => utcNow.Date > ddn.TimeOfOccurenceUtc.Date);
		if (!toRemove.Any())
			return;

		dbContext.DdNews.RemoveRange(toRemove);
		await dbContext.SaveChangesAsync();
	}
}
