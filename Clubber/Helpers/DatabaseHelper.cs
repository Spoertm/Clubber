using Clubber.Models;
using Clubber.Services;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class DatabaseHelper
	{
		private readonly IOService _iOService;
		private readonly WebService _webService;

		public DatabaseHelper(IOService iOService, WebService webService)
		{
			_iOService = iOService;
			_webService = webService;

			Database = _iOService.GetDatabase();
		}

		public List<DdUser> Database { get; }

		public async Task<(bool Success, string Message)> RegisterUser(uint lbId, SocketGuildUser user)
		{
			try
			{
				uint[] playerRequest = { lbId };

				LeaderboardUser lbPlayer = (await _webService.GetLbPlayers(playerRequest))[0];
				DdUser newDdUser = new(user.Id, lbPlayer.Id);
				Database.Add(newDdUser);
				await _iOService.UpdateAndBackupDbFile(Database, $"Add {user.Username}\n{newDdUser}\nTotal users: {Database.Count}");
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

			Database.Remove(toRemove);
			await _iOService.UpdateAndBackupDbFile(Database, $"Remove {user.Username}\n{toRemove}\nTotal users: {Database.Count}");

			return true;
		}

		public async Task<bool> RemoveUser(ulong discordId)
		{
			DdUser? toRemove = GetDdUserByDiscordId(discordId);
			if (toRemove is null)
				return false;

			Database.Remove(toRemove);
			await _iOService.UpdateAndBackupDbFile(Database, $"Remove {toRemove}\nTotal users: {Database.Count}");

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
	}
}
