using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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

	public async Task<Result> RegisterUser(uint lbId, SocketGuildUser user)
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
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		if (dbContext.DdPlayers.FirstOrDefault(ddp => ddp.DiscordId == userId) is not { } ddUser)
		{
			return Result.Failure("Couldn't find user in database.");
		}

		ddUser.TwitchUsername = twitchUsername;
		await dbContext.SaveChangesAsync();
		return Result.Success();
	}

	public async Task<Result> UnregisterTwitch(ulong userId)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		if (dbContext.DdPlayers.FirstOrDefault(ddp => ddp.DiscordId == userId) is not { } ddUser)
		{
			return Result.Failure("Couldn't find user in database.");
		}

		ddUser.TwitchUsername = null;
		await dbContext.SaveChangesAsync();
		return Result.Success(ddUser);
	}

	public async Task<bool> RemoveUser(SocketGuildUser user)
		=> await RemoveUser(user.Id);

	public async Task<bool> RemoveUser(ulong discordId)
	{
		DdUser? toRemove = GetDdUserBy(discordId);
		if (toRemove is null)
		{
			return false;
		}

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
		{
			return;
		}

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
		{
			return (currentBestSplits, superiorNewSplits.ToArray());
		}

		await dbContext.SaveChangesAsync();
		return (currentBestSplits, superiorNewSplits.ToArray());
	}

	public async Task<(HomingPeakRun[] OldTopPeaks, HomingPeakRun? NewPeakRun)> UpdateTopHomingPeaksIfNeeded(HomingPeakRun runToBeChecked)
	{
		using IServiceScope scope = _scopeFactory.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		HomingPeakRun[] currentTopPeaks = await dbContext.TopHomingPeaks
			.AsNoTracking()
			.OrderByDescending(thp => thp.HomingPeak)
			.ToArrayAsync();

		HomingPeakRun? oldPlayerRun = await dbContext.TopHomingPeaks.FirstOrDefaultAsync(hpr => hpr.PlayerLeaderboardId == runToBeChecked.PlayerLeaderboardId);
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
			EntityEntry<HomingPeakRun> response = await dbContext.TopHomingPeaks.AddAsync(runToBeChecked);
			Log.Information("Added new top homing peak run:\n{@NewRun}", response.Entity);
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
