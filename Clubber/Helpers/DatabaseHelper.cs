using Clubber.Configuration;
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
		private readonly DiscordHelper _discordHelper;
		private readonly IOService _ioService;
		private readonly WebService _webService;

		public DatabaseHelper(DiscordHelper discordHelper, IOService ioService, WebService webService)
		{
			_discordHelper = discordHelper;
			_ioService = ioService;
			_webService = webService;

			Directory.CreateDirectory(Path.GetDirectoryName(DatabaseFilePath)!);
			string latestAttachmentUrl = _discordHelper.GetLatestAttachmentUrlFromChannel(Config.DatabaseBackupChannelId).Result;
			string databaseJson = _webService.RequestStringAsync(latestAttachmentUrl).Result;
			File.WriteAllText(databaseJson, DatabaseFilePath);
			Database = _ioService.DeserializeObject<List<DdUser>>(databaseJson);
		}

		public List<DdUser> Database { get; }
		public string DatabaseFilePath => Path.Combine(AppContext.BaseDirectory, "Database", "Database.json");

		public async Task<(bool Success, string Message)> RegisterUser(uint lbId, SocketGuildUser user)
		{
			try
			{
				uint[] playerRequest = { lbId };

				LeaderboardUser lbPlayer = (await _webService.GetLbPlayers(playerRequest))[0];
				DdUser newDdUser = new(user.Id, lbPlayer.Id);
				Database.Add(newDdUser);
				await UpdateAndBackupDbFile($"Add {user.Username}\n{newDdUser}\nTotal users: {Database.Count}");
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
			await UpdateAndBackupDbFile($"Remove {user.Username}\n{toRemove}\nTotal users: {Database.Count}");
			return true;
		}

		public async Task<bool> RemoveUser(ulong discordId)
		{
			DdUser? toRemove = GetDdUserByDiscordId(discordId);
			if (toRemove is null)
				return false;

			Database.Remove(toRemove);
			await UpdateAndBackupDbFile($"Remove {toRemove}\nTotal users: {Database.Count}");
			return true;
		}

		private async Task UpdateAndBackupDbFile(string? text = null)
		{
			await _ioService.WriteObjectToFile(Database, DatabaseFilePath);
			await _discordHelper.SendFileToChannel(DatabaseFilePath, Config.DatabaseBackupChannelId, text);
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
