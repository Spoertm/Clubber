using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Clubber.Domain.Repositories;

public sealed class LeaderboardRepository(DbService dbContext) : ILeaderboardRepository
{
	public async Task<EntryResponse[]> GetCachedEntriesAsync()
	{
		return await dbContext.LeaderboardCache.AsNoTracking().ToArrayAsync();
	}

	public async Task UpdateCacheAsync(ICollection<EntryResponse> entries)
	{
		await dbContext.LeaderboardCache.ExecuteDeleteAsync();
		await dbContext.LeaderboardCache.AddRangeAsync(entries);
		await dbContext.SaveChangesAsync();
	}

	public async Task<BestSplit[]> GetBestSplitsAsync()
	{
		return await dbContext.BestSplits.AsNoTracking().OrderBy(s => s.Time).ToArrayAsync();
	}

	public async Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsAsync(
		IReadOnlyCollection<Split> splits, DdStatsFullRunResponse run, string description)
	{
		Dictionary<string, BestSplit> currentBestSplits = await dbContext.BestSplits
			.AsNoTracking()
			.ToDictionaryAsync(cbs => cbs.Name);

		List<BestSplit> updatedSplits = [];

		foreach (Split newSplit in splits)
		{
			if (currentBestSplits.TryGetValue(newSplit.Name, out BestSplit? currentBest) && newSplit.Value <= currentBest.Value)
				continue;

			if (currentBest is null)
			{
				BestSplit newBest = new()
				{
					Name = newSplit.Name,
					Time = newSplit.Time,
					Value = newSplit.Value,
					Description = description,
					GameInfo = run.GameInfo,
				};

				updatedSplits.Add(newBest);
				dbContext.BestSplits.Add(newBest);
			}
			else
			{
				BestSplit toUpdate = await dbContext.BestSplits.FindAsync(newSplit.Name)
					?? throw new InvalidOperationException($"BestSplit {newSplit.Name} not found");

				toUpdate.Value = newSplit.Value;
				toUpdate.Time = newSplit.Time;
				toUpdate.Description = description;
				toUpdate.GameInfo = run.GameInfo;
				updatedSplits.Add(toUpdate);
			}
		}

		if (updatedSplits.Count != 0)
			await dbContext.SaveChangesAsync();

		return ([.. currentBestSplits.Values], updatedSplits.ToArray());
	}

	public async Task<HomingPeakRun[]> GetTopHomingPeaksAsync()
	{
		return await dbContext.TopHomingPeaks
			.AsNoTracking()
			.OrderByDescending(pr => pr.HomingPeak)
			.ToArrayAsync();
	}

	public async Task<(HomingPeakRun? OldRun, HomingPeakRun? NewRun)> UpdateTopHomingPeakAsync(HomingPeakRun run)
	{
		HomingPeakRun? existing = await dbContext.TopHomingPeaks
			.FirstOrDefaultAsync(hpr => hpr.PlayerLeaderboardId == run.PlayerLeaderboardId);

		if (existing is not null && run.HomingPeak <= existing.HomingPeak)
			return (existing, null);

		HomingPeakRun? oldRun = existing is not null ? new HomingPeakRun
		{
			Id = existing.Id,
			PlayerLeaderboardId = existing.PlayerLeaderboardId,
			PlayerName = existing.PlayerName,
			HomingPeak = existing.HomingPeak,
			Source = existing.Source,
		} : null;

		if (existing is not null)
		{
			existing.HomingPeak = run.HomingPeak;
			existing.Source = run.Source;
			existing.PlayerName = run.PlayerName;
			Log.Information("Updating top homing peak for {PlayerName}:\n{@NewRun}", run.PlayerName, run);
		}
		else
		{
			dbContext.TopHomingPeaks.Add(run);
			Log.Information("Added new top homing peak run:\n{@NewRun}", run);
		}

		await dbContext.SaveChangesAsync();
		return (oldRun, run);
	}
}
