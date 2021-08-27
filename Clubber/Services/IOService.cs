using Clubber.Models;
using Clubber.Models.Responses;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public class IOService
	{
		private readonly string _databaseFilePath;
		private readonly string _lbEntriesCachePath;
		private readonly LoggingService _loggingService;
		private readonly WebService _webService;

		public IOService(WebService webService, LoggingService loggingService)
		{
			_databaseFilePath = Path.Combine(AppContext.BaseDirectory, "Database", "Database.json");
			_lbEntriesCachePath = Path.Combine(AppContext.BaseDirectory, "LeaderboardCache.json");

			_webService = webService;
			_loggingService = loggingService;
		}

		public async Task UpdateAndBackupDbFile(List<DdUser> list, string? text = null)
		{
			string fileContents = JsonConvert.SerializeObject(list, Formatting.Indented);
			await File.WriteAllTextAsync(_databaseFilePath, fileContents);

			await _webService.BackupDbFile(_databaseFilePath, text);
		}

		public List<DdUser> GetDatabase()
		{
			string dbString = File.ReadAllText(_databaseFilePath);
			return JsonConvert.DeserializeObject<List<DdUser>>(dbString);
		}

		public async Task GetDatabaseFileIntoFolder()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_databaseFilePath)!);
				string dbString = await _webService.GetLatestFileContentsFromChannel(Constants.DatabaseBackupChannelId);
				await File.WriteAllTextAsync(_databaseFilePath, dbString);
			}
			catch (Exception ex)
			{
				await _loggingService.LogAsync(new(LogSeverity.Critical, "Startup", "Failed to get database file into folder.", ex));
				Program.StopBot();
			}
		}

		public void GetLbEntriesCacheFromDiscordAndSaveToFile()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_lbEntriesCachePath)!);
			string dbString = _webService.GetLatestFileContentsFromChannel(Constants.LbEntriesCacheChannelId).Result;
			File.WriteAllText(_lbEntriesCachePath, dbString);
		}

		public async Task<List<EntryResponse>> GetLbEntriesCacheFromFile()
			=> JsonConvert.DeserializeObject<List<EntryResponse>>(await File.ReadAllTextAsync(_lbEntriesCachePath));

		public async Task UpdateLbEntriesCache(string filePath, string? text)
		{
			await File.WriteAllTextAsync(filePath, text);
			await _webService.BackupLbEntriesCache(filePath, text);
		}
	}
}
