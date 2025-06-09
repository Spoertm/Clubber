using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Serilog;

namespace Clubber.Domain.Helpers;

public sealed class DatabaseHelper(DbService dbContext) : IDatabaseHelper
{
	public async Task<List<DdUser>> GetRegisteredUsers()
	{
		return await dbContext.DdPlayers.AsNoTracking().ToListAsync();
	}

	public async Task<List<DdUser>> GetRegisteredUsers(IEnumerable<ulong> discordIds)
	{
		return await dbContext.DdPlayers
			.AsNoTracking()
			.Where(ddp => discordIds.Contains(ddp.DiscordId))
			.ToListAsync();
	}

	public async Task<int> GetRegisteredUserCount()
	{
		return await dbContext.DdPlayers.CountAsync();
	}

	public async Task<Result> RegisterUser(uint lbId, ulong discordId)
	{
		try
		{
			DdUser newDdUser = new(discordId, (int)lbId);

			await dbContext.AddAsync(newDdUser);
			await dbContext.SaveChangesAsync();
			return Result.Success();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error registering user {DiscordId} with {LbId}", discordId, lbId);
			return ex switch
			{
				DbUpdateException => Result.Failure("Database error."),
				_ => Result.Failure("Internal error."),
			};
		}
	}

	public async Task<Result> RegisterTwitch(ulong userId, string twitchUsername)
	{
		if (await FindRegisteredUser(userId) is not { } ddUser)
		{
			return Result.Failure("User isn't registered.");
		}

		ddUser.TwitchUsername = twitchUsername;
		await dbContext.SaveChangesAsync();
		return Result.Success();
	}

	public async Task<Result> UnregisterTwitch(ulong userId)
	{
		if (await FindRegisteredUser(userId) is not { } ddUser)
		{
			return Result.Failure("User isn't registered.");
		}

		ddUser.TwitchUsername = null;
		await dbContext.SaveChangesAsync();
		return Result.Success(ddUser);
	}

	public async Task<bool> RemoveUser(ulong discordId)
	{
		DdUser? toRemove = await FindRegisteredUser(discordId);
		if (toRemove is null)
		{
			return false;
		}

		dbContext.Remove(toRemove);
		await dbContext.SaveChangesAsync();
		return true;
	}

	public async Task<DdUser?> FindRegisteredUser(int lbId)
	{
		return await dbContext.DdPlayers.FindAsync(lbId);
	}

	public async Task<DdUser?> FindRegisteredUser(ulong discordId)
	{
		return await dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.DiscordId == discordId);
	}

	public async Task UpdateLeaderboardCache(ICollection<EntryResponse> newEntries)
	{
		await dbContext.LeaderboardCache.ExecuteDeleteAsync();
		await dbContext.LeaderboardCache.AddRangeAsync(newEntries);
		await dbContext.SaveChangesAsync();
	}

	public async Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth)
	{
		DdNewsItem newItem = new(oldEntry.Id, oldEntry, newEntry, DateTime.UtcNow, nth);
		await dbContext.DdNews.AddAsync(newItem);
		await dbContext.SaveChangesAsync();
	}

	public async Task CleanUpNewsItems()
	{
		DateTime utcNow = DateTime.UtcNow;
		IQueryable<DdNewsItem> toRemove = dbContext.DdNews.Where(ddn => utcNow - ddn.TimeOfOccurenceUtc >= TimeSpan.FromDays(1));
		if (await toRemove.AnyAsync())
		{
			dbContext.DdNews.RemoveRange(toRemove);
			await dbContext.SaveChangesAsync();
		}
	}

	public async Task<bool> TwitchUsernameIsRegistered(string twitchUsername)
	{
		return await dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.TwitchUsername == twitchUsername) is not null;
	}

	public async Task<BestSplit[]> GetBestSplits()
	{
		return await dbContext.BestSplits.AsNoTracking().OrderBy(s => s.Time).ToArrayAsync();
	}

	/// <summary>
	/// Updates the current best splits as necessary if the provided splits are superior.
	/// </summary>
	/// <returns>Tuple containing all the old best splits and the updated new splits.</returns>
	public async Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsIfNeeded(
		IReadOnlyCollection<Split> splitsToBeChecked, DdStatsFullRunResponse ddstatsRun, string description)
	{
		BestSplit[] currentBestSplits = await dbContext.BestSplits.AsNoTracking().ToArrayAsync();
		List<BestSplit> superiorNewSplits = [];
		foreach (Split newSplit in splitsToBeChecked)
		{
			BestSplit? currentBestSplit = Array.Find(currentBestSplits, cbs => cbs.Name == newSplit.Name);
			BestSplit newBest = new()
			{
				Name = newSplit.Name,
				Time = newSplit.Time,
				Value = newSplit.Value,
				Description = description,
				GameInfo = ddstatsRun.GameInfo,
			};

			if (currentBestSplit is null)
			{
				superiorNewSplits.Add(newBest);
				await dbContext.BestSplits.AddAsync(newBest);
			}
			else if (newSplit.Value > currentBestSplit.Value)
			{
				superiorNewSplits.Add(newBest);
				dbContext.BestSplits.Update(newBest);
			}
		}

		if (superiorNewSplits.Count == 0)
		{
			return (currentBestSplits, superiorNewSplits.ToArray());
		}

		await dbContext.SaveChangesAsync();
		return (currentBestSplits, superiorNewSplits.ToArray());
	}

	public async Task<(HomingPeakRun? OldRun, HomingPeakRun? NewRun)> UpdateTopHomingPeaksIfNeeded(HomingPeakRun runToBeChecked)
	{
		HomingPeakRun? oldRun = await dbContext.TopHomingPeaks.AsNoTracking()
			.FirstOrDefaultAsync(hpr => hpr.PlayerLeaderboardId == runToBeChecked.PlayerLeaderboardId);
		if (oldRun != null)
		{
			if (runToBeChecked.HomingPeak > oldRun.HomingPeak)
			{
				runToBeChecked.Id = oldRun.Id;
				dbContext.TopHomingPeaks.Update(runToBeChecked);
				Log.Information("Updating top homing peak for {PlayerName}:\n{@NewRun}", runToBeChecked.PlayerName, runToBeChecked);
			}
			else
			{
				return (oldRun, null);
			}
		}
		else
		{
			EntityEntry<HomingPeakRun> response = await dbContext.TopHomingPeaks.AddAsync(runToBeChecked);
			Log.Information("Added new top homing peak run:\n{@NewRun}", response.Entity);
		}

		await dbContext.SaveChangesAsync();

		return (oldRun, runToBeChecked);
	}

	public async Task<HomingPeakRun[]> GetTopHomingPeaks()
	{
		return await dbContext.TopHomingPeaks
			.AsNoTracking()
			.OrderByDescending(pr => pr.HomingPeak)
			.ToArrayAsync();
	}
}
