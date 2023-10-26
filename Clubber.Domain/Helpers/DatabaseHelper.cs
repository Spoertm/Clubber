using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Serilog;

namespace Clubber.Domain.Helpers;

public class DatabaseHelper : IDatabaseHelper
{
	private readonly IWebService _webService;
	private readonly DbService _dbContext;

	public DatabaseHelper(IWebService webService, DbService dbContext)
	{
		_webService = webService;
		_dbContext = dbContext;
	}

	public async Task<List<DdUser>> GetEntireDatabase()
	{
		return await _dbContext.DdPlayers.AsNoTracking().ToListAsync();
	}

	public async Task<Result> RegisterUser(uint lbId, SocketGuildUser user)
	{
		try
		{
			uint[] playerRequest = { lbId };

			EntryResponse lbPlayer = (await _webService.GetLbPlayers(playerRequest))[0];
			DdUser newDdUser = new(user.Id, lbPlayer.Id);

			await _dbContext.AddAsync(newDdUser);
			await _dbContext.SaveChangesAsync();
			return Result.Success();
		}
		catch (Exception ex)
		{
			return ex switch
			{
				ClubberException     => Result.Failure(ex.Message),
				HttpRequestException => Result.Failure("DD servers are most likely down."),
				IOException          => Result.Failure("IO error."),
				_                    => Result.Failure("No reason specified."),
			};
		}
	}

	public async Task<Result> RegisterTwitch(ulong userId, string twitchUsername)
	{
		if (await _dbContext.DdPlayers.FirstOrDefaultAsync(ddp => ddp.DiscordId == userId) is not { } ddUser)
		{
			return Result.Failure("Couldn't find user in database.");
		}

		ddUser.TwitchUsername = twitchUsername;
		await _dbContext.SaveChangesAsync();
		return Result.Success();
	}

	public async Task<Result> UnregisterTwitch(ulong userId)
	{
		if (await _dbContext.DdPlayers.FirstOrDefaultAsync(ddp => ddp.DiscordId == userId) is not { } ddUser)
		{
			return Result.Failure("Couldn't find user in database.");
		}

		ddUser.TwitchUsername = null;
		await _dbContext.SaveChangesAsync();
		return Result.Success(ddUser);
	}

	public async Task<bool> RemoveUser(SocketGuildUser user)
		=> await RemoveUser(user.Id);

	public async Task<bool> RemoveUser(ulong discordId)
	{
		DdUser? toRemove = await GetDdUserBy(discordId);
		if (toRemove is null)
		{
			return false;
		}

		_dbContext.Remove(toRemove);
		await _dbContext.SaveChangesAsync();
		return true;
	}

	public async Task<DdUser?> GetDdUserBy(int lbId)
	{
		return await _dbContext.DdPlayers.FindAsync(lbId);
	}

	public async Task<DdUser?> GetDdUserBy(ulong discordId)
	{
		return await _dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.DiscordId == discordId);
	}

	public async Task UpdateLeaderboardCache(List<EntryResponse> newEntries)
	{
		await _dbContext.LeaderboardCache.ExecuteDeleteAsync();
		await _dbContext.LeaderboardCache.AddRangeAsync(newEntries);
		await _dbContext.SaveChangesAsync();
	}

	public async Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth)
	{
		DdNewsItem newItem = new(oldEntry.Id, oldEntry, newEntry, DateTime.UtcNow, nth);
		await _dbContext.DdNews.AddAsync(newItem);
		await _dbContext.SaveChangesAsync();
	}

	public async Task CleanUpNewsItems()
	{
		DateTime utcNow = DateTime.UtcNow;
		IQueryable<DdNewsItem> toRemove = _dbContext.DdNews.Where(ddn => utcNow - ddn.TimeOfOccurenceUtc >= TimeSpan.FromDays(1));
		if (toRemove.Any())
		{
			_dbContext.DdNews.RemoveRange(toRemove);
			await _dbContext.SaveChangesAsync();
		}
	}

	public async Task<bool> TwitchUsernameIsRegistered(string twitchUsername)
	{
		return await _dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.TwitchUsername == twitchUsername) is not null;
	}

	public async Task<BestSplit[]> GetBestSplits()
	{
		return await _dbContext.BestSplits.AsNoTracking().OrderBy(s => s.Time).ToArrayAsync();
	}

	/// <summary>
	/// Updates the current best splits as necessary if the provided splits are superior.
	/// </summary>
	/// <returns>Tuple containing all the old best splits and the updated new splits.</returns>
	public async Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsIfNeeded(Split[] splitsToBeChecked, DdStatsFullRunResponse ddstatsRun, string description)
	{
		BestSplit[] currentBestSplits = await _dbContext.BestSplits.AsNoTracking().ToArrayAsync();
		List<BestSplit> superiorNewSplits = new();
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
				await _dbContext.BestSplits.AddAsync(newBest);
			}
			else if (newSplit.Value > currentBestSplit.Value)
			{
				superiorNewSplits.Add(newBest);
				_dbContext.BestSplits.Update(newBest);
			}
		}

		if (superiorNewSplits.Count == 0)
		{
			return (currentBestSplits, superiorNewSplits.ToArray());
		}

		await _dbContext.SaveChangesAsync();
		return (currentBestSplits, superiorNewSplits.ToArray());
	}

	public async Task<(HomingPeakRun? OldRun, HomingPeakRun? NewRun)> UpdateTopHomingPeaksIfNeeded(HomingPeakRun runToBeChecked)
	{
		HomingPeakRun? oldRun = await _dbContext.TopHomingPeaks.AsNoTracking().FirstOrDefaultAsync(hpr => hpr.PlayerLeaderboardId == runToBeChecked.PlayerLeaderboardId);
		if (oldRun != null)
		{
			if (runToBeChecked.HomingPeak > oldRun.HomingPeak)
			{
				runToBeChecked.Id = oldRun.Id;
				_dbContext.TopHomingPeaks.Update(runToBeChecked);
				Log.Information("Updating top homing peak for {PlayerName}:\n{@NewRun}", runToBeChecked.PlayerName, runToBeChecked);
			}
			else
			{
				return (oldRun, null);
			}
		}
		else
		{
			EntityEntry<HomingPeakRun> response = await _dbContext.TopHomingPeaks.AddAsync(runToBeChecked);
			Log.Information("Added new top homing peak run:\n{@NewRun}", response.Entity);
		}

		await _dbContext.SaveChangesAsync();

		return (oldRun, runToBeChecked);
	}

	public async Task<HomingPeakRun[]> GetTopHomingPeaks()
	{
		return await _dbContext.TopHomingPeaks
			.AsNoTracking()
			.OrderByDescending(pr => pr.HomingPeak)
			.ToArrayAsync();
	}
}
