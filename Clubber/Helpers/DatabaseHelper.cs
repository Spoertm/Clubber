using Clubber.Models;
using Clubber.Services;
using Discord.WebSocket;
using System.Collections.Generic;
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

		public async Task RegisterUser(uint lbId, SocketGuildUser user)
		{
			LeaderboardUser lbPlayer = _webService.GetLbPlayers(new[] { lbId }).Result[0];
			DdUser newDdUser = new(user.Id, lbPlayer.Id);
			Database.Add(newDdUser);
			await _iOService.UpdateAndBackupDbFile(Database, $"Add {user.Username}\n{newDdUser}\nTotal users: {Database.Count}");
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
