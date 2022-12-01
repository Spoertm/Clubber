﻿using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clubber.Domain.Helpers;

public class DatabaseHelper : IDatabaseHelper
{
	private readonly IWebService _webService;
	private readonly IServiceScopeFactory _scopeFactory;

	public DatabaseHelper(IWebService webService, IServiceScopeFactory scopeFactory)
	{
		_webService = webService;
		_scopeFactory = scopeFactory;
	}

	public async Task<List<DdUser>> GetEntireDatabase()
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
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
			await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			await dbContext.AddAsync(newDdUser);
			await dbContext.SaveChangesAsync();
			return (true, string.Empty);
		}
		catch (Exception ex)
		{
			return ex switch
			{
				ClubberException     => (false, ex.Message),
				HttpRequestException => (false, "DD servers are most likely down."),
				IOException          => (false, "IO error."),
				_                    => (false, "No reason specified."),
			};
		}
	}

	public async Task<(bool Success, string Message)> RegisterTwitch(ulong userId, string twitchUsername)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		if (dbContext.DdPlayers.FirstOrDefault(ddp => ddp.DiscordId == userId) is not { } ddUser)
			return (false, "Couldn't find user in database.");

		ddUser.TwitchUsername = twitchUsername;
		await dbContext.SaveChangesAsync();
		return (true, string.Empty);
	}

	public async Task<(bool Success, string Message)> UnregisterTwitch(ulong userId)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		if (dbContext.DdPlayers.FirstOrDefault(ddp => ddp.DiscordId == userId) is not { } ddUser)
			return (false, "Couldn't find user in database.");

		ddUser.TwitchUsername = null;
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
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		dbContext.Remove(toRemove);
		await dbContext.SaveChangesAsync();
		return true;
	}

	public DdUser? GetDdUserBy(int lbId)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return dbContext.DdPlayers.FirstOrDefault(ddp => ddp.LeaderboardId == lbId);
	}

	public DdUser? GetDdUserBy(ulong discordId)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return dbContext.DdPlayers.FirstOrDefault(ddp => ddp.DiscordId == discordId);
	}

	public async Task UpdateLeaderboardCache(List<EntryResponse> newEntries)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		await dbContext.LeaderboardCache.ExecuteDeleteAsync();
		await dbContext.LeaderboardCache.AddRangeAsync(newEntries);
		await dbContext.SaveChangesAsync();
	}

	public async Task AddDdNewsItem(EntryResponse oldEntry, EntryResponse newEntry, int nth)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();

		DdNewsItem newItem = new(oldEntry.Id, oldEntry, newEntry, DateTime.UtcNow, nth);
		await dbContext.DdNews.AddAsync(newItem);
		await dbContext.SaveChangesAsync();
	}

	public async Task CleanUpNewsItems()
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		DateTime utcNow = DateTime.UtcNow;
		IQueryable<DdNewsItem> toRemove = dbContext.DdNews.Where(ddn => utcNow - ddn.TimeOfOccurenceUtc >= TimeSpan.FromDays(1));
		if (!toRemove.Any())
			return;

		dbContext.DdNews.RemoveRange(toRemove);
		await dbContext.SaveChangesAsync();
	}

	public async Task<bool> TwitchUsernameIsRegistered(string twitchUsername)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.DdPlayers.AsNoTracking().FirstOrDefaultAsync(ddp => ddp.TwitchUsername == twitchUsername) is not null;
	}

	public async Task<BestSplit[]> GetBestSplits()
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.BestSplits.AsNoTracking().OrderBy(s => s.Time).ToArrayAsync();
	}

	/// <summary>
	/// Updates the current best splits as necessary if the provided splits are superior.
	/// </summary>
	/// <returns>Tuple containing all the old best splits and the updated new splits.</returns>
	public async Task<(BestSplit[] OldBestSplits, BestSplit[] UpdatedBestSplits)> UpdateBestSplitsIfNeeded(Split[] splitsToBeChecked, DdStatsFullRunResponse ddstatsRun, string description)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		BestSplit[] currentBestSplits = await dbContext.BestSplits.AsNoTracking().ToArrayAsync();
		List<BestSplit> superiorNewSplits = new();
		foreach (Split newSplit in splitsToBeChecked)
		{
			BestSplit? currentBestSplit = currentBestSplits.FirstOrDefault(cbs => cbs.Name == newSplit.Name);
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
			return (currentBestSplits, superiorNewSplits.ToArray());

		await dbContext.SaveChangesAsync();
		return (currentBestSplits, superiorNewSplits.ToArray());
	}

	public async Task<(HomingPeakRun[] OldTopPeaks, HomingPeakRun? NewPeakRun)> UpdateTopHomingPeaksIfNeeded(HomingPeakRun runToBeChecked)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		HomingPeakRun[] currentTopPeaks = await dbContext.TopHomingPeaks.AsNoTracking().OrderByDescending(thp => thp.HomingPeak).ToArrayAsync();

		HomingPeakRun? oldPlayerRun = Array.Find(currentTopPeaks, hpr => hpr.PlayerLeaderboardId == runToBeChecked.PlayerLeaderboardId);
		if (oldPlayerRun != null)
		{
			if (runToBeChecked.HomingPeak > oldPlayerRun.HomingPeak)
			{
				runToBeChecked.Id = oldPlayerRun.Id;
				dbContext.TopHomingPeaks.Update(runToBeChecked);
			}
			else
			{
				return (currentTopPeaks, null);
			}
		}
		else
		{
			await dbContext.TopHomingPeaks.AddAsync(runToBeChecked);
		}

		await dbContext.SaveChangesAsync();

		return (currentTopPeaks, runToBeChecked);
	}

	public async Task<HomingPeakRun[]> GetTopHomingPeaks()
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		return await dbContext.TopHomingPeaks.AsNoTracking().OrderByDescending(pr => pr.HomingPeak).ToArrayAsync();
	}
}