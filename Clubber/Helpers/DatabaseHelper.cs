using Clubber.Models;
using Clubber.Services;
using Discord.WebSocket;
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

			Database = dbContext.DdPlayers.ToList();
		}

		public List<DdUser> Database { get; }

		public async Task<(bool Success, string Message)> RegisterUser(uint lbId, SocketGuildUser user)
		{
			try
			{
				uint[] playerRequest = { lbId };

				LeaderboardUser lbPlayer = (await _webService.GetLbPlayers(playerRequest))[0];
				DdUser newDdUser = new(user.Id, lbPlayer.Id);
				await _dbContext.AddAsync(newDdUser);
				await _dbContext.SaveChangesAsync();
				Database.Add(newDdUser);
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
			Database.Remove(toRemove);
			return true;
		}

		public async Task<bool> RemoveUser(ulong discordId)
		{
			DdUser? toRemove = GetDdUserByDiscordId(discordId);
			if (toRemove is null)
				return false;

			_dbContext.Remove(toRemove);
			await _dbContext.SaveChangesAsync();
			Database.Remove(toRemove);
			return true;
		}

		public DdUser? GetDdUserByDiscordId(ulong discordId)
		{
			for (int i = 0; i < Database.Count; i++)
			{
				if (Database[i].DiscordId == discordId)
					return Database[i];
			}

			return null;
		}

		public DdUser? GetDdUserByLbId(int lbId)
		{
			for (int i = 0; i < Database.Count; i++)
			{
				if (Database[i].LeaderboardId == lbId)
					return Database[i];
			}

			return null;
		}
	}
}
