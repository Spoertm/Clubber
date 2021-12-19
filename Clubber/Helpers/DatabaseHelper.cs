using Clubber.Models;
using Clubber.Models.Responses;
using Clubber.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class DatabaseHelper : IDatabaseHelper
	{
		private readonly IWebService _webService;
		private readonly DatabaseService _dbContext;

		public DatabaseHelper(IWebService webService, DatabaseService dbContext)
		{
			_webService = webService;
			_dbContext = dbContext;

			DdUserDatabase = dbContext.DdPlayers.ToList();
			LeaderboardCache = dbContext.LeaderboardCache.ToList();
		}

		public List<DdUser> DdUserDatabase { get; }
		public List<EntryResponse> LeaderboardCache { get; }

		public async Task<(bool Success, string Message)> RegisterUser(uint lbId, SocketGuildUser user)
		{
			try
			{
				uint[] playerRequest = { lbId };

				EntryResponse lbPlayer = (await _webService.GetLbPlayers(playerRequest))[0];
				DdUser newDdUser = new(user.Id, lbPlayer.Id);
				await _dbContext.AddAsync(newDdUser);
				await _dbContext.SaveChangesAsync();
				DdUserDatabase.Add(newDdUser);
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

		public async Task<bool> RemoveUser(SocketGuildUser user)
		{
			DdUser? toRemove = GetDdUserByDiscordId(user.Id);
			if (toRemove is null)
				return false;

			_dbContext.Remove(toRemove);
			await _dbContext.SaveChangesAsync();
			DdUserDatabase.Remove(toRemove);
			return true;
		}

		public async Task<bool> RemoveUser(ulong discordId)
		{
			DdUser? toRemove = GetDdUserByDiscordId(discordId);
			if (toRemove is null)
				return false;

			_dbContext.Remove(toRemove);
			await _dbContext.SaveChangesAsync();
			DdUserDatabase.Remove(toRemove);
			return true;
		}

		public DdUser? GetDdUserByDiscordId(ulong discordId)
		{
			for (int i = 0; i < DdUserDatabase.Count; i++)
			{
				if (DdUserDatabase[i].DiscordId == discordId)
					return DdUserDatabase[i];
			}

			return null;
		}

		public DdUser? GetDdUserByLbId(int lbId)
		{
			for (int i = 0; i < DdUserDatabase.Count; i++)
			{
				if (DdUserDatabase[i].LeaderboardId == lbId)
					return DdUserDatabase[i];
			}

			return null;
		}

		public async Task UpdateLeaderboardCache(List<EntryResponse> newEntries)
		{
			await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE leaderboard_cache");
			await _dbContext.LeaderboardCache.AddRangeAsync(newEntries);
			await _dbContext.SaveChangesAsync();
			LeaderboardCache.Clear();
			LeaderboardCache.AddRange(newEntries);
		}
	}
}
