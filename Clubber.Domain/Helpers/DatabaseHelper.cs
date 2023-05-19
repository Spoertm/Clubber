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
	private readonly AppDbContext _appDbContext;

	public DatabaseHelper(IWebService webService, AppDbContext appDbContext)
	{
		_webService = webService;
		_appDbContext = appDbContext;
	}

	public async Task<List<DdUser>> GetEntireDatabase()
	{
		return await _appDbContext.DdPlayers.AsNoTracking().ToListAsync();
	}

	public async Task<Result> RegisterUser(uint lbId, SocketGuildUser user)
	{
		try
		{
			uint[] playerRequest = { lbId };

			EntryResponse lbPlayer = (await _webService.GetLbPlayers(playerRequest))[0];
			DdUser newDdUser = new(user.Id, lbPlayer.Id);

			await _appDbContext.AddAsync(newDdUser);
			await _appDbContext.SaveChangesAsync();
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
		if (await _appDbContext.DdPlayers.FirstOrDefaultAsync(ddp => ddp.DiscordId == userId) is not { } ddUser)
		{
			return Result.Failure("Couldn't find user in database.");
		}

		ddUser.TwitchUsername = twitchUsername;
		await _appDbContext.SaveChangesAsync();
		return Result.Success();
	}

	public async Task<Result> UnregisterTwitch(ulong userId)
	{
		if (await _appDbContext.DdPlayers.FirstOrDefaultAsync(ddp => ddp.DiscordId == userId) is not { } ddUser)
		{
			return Result.Failure("Couldn't find user in database.");
		}

		ddUser.TwitchUsername = null;
		await _appDbContext.SaveChangesAsync();
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

		_appDbContext.Remove(toRemove);
		await _appDbContext.SaveChangesAsync();
		return true;
	}

	public async Task<DdUser?> GetDdUserBy(int lbId)
	{
		return await _appDbContext.DdPlayers.FindAsync(lbId);
	}

	public async Task<DdUser?> GetDdUserBy(ulong discordId)
	{
		return await _appDbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.DiscordId == discordId);
	}

	public async Task UpdateLeaderboardCache(List<EntryResponse> newEntries)
	{
		await _appDbContext.LeaderboardCache.ExecuteDeleteAsync();
		await _appDbContext.LeaderboardCache.AddRangeAsync(newEntries);
		await _appDbContext.SaveChangesAsync();
	}

	public async Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth)
	{
		DdNewsItem newItem = new(oldEntry.Id, oldEntry, newEntry, DateTime.UtcNow, nth);
		await _appDbContext.DdNews.AddAsync(newItem);
		await _appDbContext.SaveChangesAsync();
	}

	public async Task CleanUpNewsItems()
	{
		DateTime utcNow = DateTime.UtcNow;
		IQueryable<DdNewsItem> toRemove = _appDbContext.DdNews.Where(ddn => utcNow - ddn.TimeOfOccurenceUtc >= TimeSpan.FromDays(1));
		if (toRemove.Any())
		{
			_appDbContext.DdNews.RemoveRange(toRemove);
			await _appDbContext.SaveChangesAsync();
		}
	}

	public async Task<bool> TwitchUsernameIsRegistered(string twitchUsername)
	{
		return await _appDbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.TwitchUsername == twitchUsername) is not null;
	}

	public async Task<BestSplit[]> GetBestSplits()
	{
		return await _appDbContext.BestSplits.AsNoTracking().OrderBy(s => s.Time).ToArrayAsync();
	}

	/// <summary>
	/// Updates the current best splits as necessary if the provided splits are superior.
	/// </summary>
	/// <returns>Tuple containing all the old best splits and the updated new splits.</returns>
	public async Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsIfNeeded(Split[] splitsToBeChecked, DdStatsFullRunResponse ddstatsRun, string description)
	{
		BestSplit[] currentBestSplits = await _appDbContext.BestSplits.AsNoTracking().ToArrayAsync();
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
				await _appDbContext.BestSplits.AddAsync(newBest);
			}
			else if (newSplit.Value > currentBestSplit.Value)
			{
				superiorNewSplits.Add(newBest);
				_appDbContext.BestSplits.Update(newBest);
			}
		}

		if (superiorNewSplits.Count == 0)
		{
			return (currentBestSplits, superiorNewSplits.ToArray());
		}

		await _appDbContext.SaveChangesAsync();
		return (currentBestSplits, superiorNewSplits.ToArray());
	}

	public async Task<(HomingPeakRun[] OldTopPeaks, HomingPeakRun? NewPeakRun)> UpdateTopHomingPeaksIfNeeded(HomingPeakRun runToBeChecked)
	{
		HomingPeakRun[] currentTopPeaks = await _appDbContext.TopHomingPeaks
			.AsNoTracking()
			.OrderByDescending(thp => thp.HomingPeak)
			.ToArrayAsync();

		HomingPeakRun? oldPlayerRun = await _appDbContext.TopHomingPeaks.FirstOrDefaultAsync(hpr => hpr.PlayerLeaderboardId == runToBeChecked.PlayerLeaderboardId);
		if (oldPlayerRun != null)
		{
			if (runToBeChecked.HomingPeak > oldPlayerRun.HomingPeak)
			{
				oldPlayerRun.PlayerName = runToBeChecked.PlayerName;
				oldPlayerRun.HomingPeak = runToBeChecked.HomingPeak;
				oldPlayerRun.Source = runToBeChecked.Source;

				Log.Information("Updating top homing peak for {PlayerName}:\n{@NewRun}", runToBeChecked.PlayerName, runToBeChecked);
			}
			else
			{
				return (currentTopPeaks, null);
			}
		}
		else
		{
			EntityEntry<HomingPeakRun> response = await _appDbContext.TopHomingPeaks.AddAsync(runToBeChecked);
			Log.Information("Added new top homing peak run:\n{@NewRun}", response.Entity);
		}

		await _appDbContext.SaveChangesAsync();

		return (currentTopPeaks, runToBeChecked);
	}

	public async Task<HomingPeakRun[]> GetTopHomingPeaks()
	{
		return await _appDbContext.TopHomingPeaks
			.AsNoTracking()
			.OrderByDescending(pr => pr.HomingPeak)
			.ToArrayAsync();
	}
}
