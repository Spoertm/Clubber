using Clubber.Domain.Models;
using Clubber.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Clubber.Domain.Repositories;

public sealed class UserRepository(DbService dbContext) : IUserRepository
{
	public async Task<List<DdUser>> GetAllAsync()
	{
		return await dbContext.DdPlayers.AsNoTracking().ToListAsync();
	}

	public async Task<List<DdUser>> GetByDiscordIdsAsync(IEnumerable<ulong> discordIds)
	{
		return await dbContext.DdPlayers
			.AsNoTracking()
			.Where(ddp => discordIds.Contains(ddp.DiscordId))
			.ToListAsync();
	}

	public async Task<int> GetCountAsync()
	{
		return await dbContext.DdPlayers.CountAsync();
	}

	public async Task<DdUser?> FindAsync(int leaderboardId)
	{
		return await dbContext.DdPlayers.FindAsync(leaderboardId);
	}

	public async Task<DdUser?> FindAsync(ulong discordId)
	{
		return await dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.DiscordId == discordId);
	}

	public async Task<bool> TwitchUsernameExistsAsync(string twitchUsername)
	{
		return await dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.TwitchUsername == twitchUsername) is not null;
	}

	public async Task<Result> RegisterAsync(uint leaderboardId, ulong discordId)
	{
		try
		{
			DdUser newDdUser = new(discordId, (int)leaderboardId);
			await dbContext.DdPlayers.AddAsync(newDdUser);
			await dbContext.SaveChangesAsync();
			return Result.Success();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error registering user {DiscordId} with {LeaderboardId}", discordId, leaderboardId);
			return ex switch
			{
				DbUpdateException => Result.Failure("Database error."),
				_ => Result.Failure("Internal error."),
			};
		}
	}

	public async Task<Result> RegisterTwitchAsync(ulong discordId, string twitchUsername)
	{
		DdUser? ddUser = await FindAsync(discordId);
		if (ddUser is null)
		{
			return Result.Failure("User isn't registered.");
		}

		dbContext.DdPlayers.Attach(ddUser);
		ddUser.TwitchUsername = twitchUsername;
		await dbContext.SaveChangesAsync();
		return Result.Success();
	}

	public async Task<Result> UnregisterTwitchAsync(ulong discordId)
	{
		DdUser? ddUser = await FindAsync(discordId);
		if (ddUser is null)
		{
			return Result.Failure("User isn't registered.");
		}

		dbContext.DdPlayers.Attach(ddUser);
		ddUser.TwitchUsername = null;
		await dbContext.SaveChangesAsync();
		return Result.Success();
	}

	public async Task<bool> RemoveAsync(ulong discordId)
	{
		DdUser? toRemove = await FindAsync(discordId);
		if (toRemove is null)
		{
			return false;
		}

		dbContext.DdPlayers.Remove(toRemove);
		await dbContext.SaveChangesAsync();
		return true;
	}
}
