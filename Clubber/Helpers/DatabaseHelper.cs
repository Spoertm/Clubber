using Clubber.Models;
using Clubber.Models.Responses;
using Clubber.Services;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
		private readonly IServiceProvider _services;

		public DatabaseHelper(IWebService webService, IServiceProvider services)
		{
			_webService = webService;
			_services = services;

			DatabaseService dbContext = _services.GetRequiredService<DatabaseService>();
			DdUserDatabase = dbContext.DdPlayers.AsNoTracking().ToList();
			LeaderboardCache = dbContext.LeaderboardCache.AsNoTracking().ToList();
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
				DatabaseService dbContext = _services.GetRequiredService<DatabaseService>();
				await dbContext.AddAsync(newDdUser);
				await dbContext.SaveChangesAsync();
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

			DatabaseService dbContext = _services.GetRequiredService<DatabaseService>();
			dbContext.Remove(toRemove);
			await dbContext.SaveChangesAsync();
			DdUserDatabase.Remove(toRemove);
			return true;
		}

		public async Task<bool> RemoveUser(ulong discordId)
		{
			DdUser? toRemove = GetDdUserByDiscordId(discordId);
			if (toRemove is null)
				return false;

			DatabaseService dbContext = _services.GetRequiredService<DatabaseService>();
			dbContext.Remove(toRemove);
			await dbContext.SaveChangesAsync();
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
			DatabaseService dbContext = _services.GetRequiredService<DatabaseService>();
			await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE leaderboard_cache");
			await dbContext.LeaderboardCache.AddRangeAsync(newEntries);
			await dbContext.SaveChangesAsync();
			LeaderboardCache.Clear();
			LeaderboardCache.AddRange(newEntries);
		}
	}
}
