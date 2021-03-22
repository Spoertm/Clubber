﻿using Clubber.Files;
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
		private readonly WebService _webService;
		private readonly LoggingService _loggingService;

		public IOService(WebService webService, LoggingService loggingService)
		{
			_databaseFilePath = Path.Combine(AppContext.BaseDirectory, "Database", "Database.json");

			_webService = webService;
			_loggingService = loggingService;
		}

		public async Task UpdateAndBackupDbFile(List<DdUser> list, string? text = null)
		{
			string fileContents = JsonConvert.SerializeObject(list, Formatting.Indented);
			await File.WriteAllTextAsync(_databaseFilePath, fileContents);

			await _webService.BackupDbFile(_databaseFilePath, text);
		}

		public async Task<List<DdUser>> GetDatabase()
		{
			string dbString = await File.ReadAllTextAsync(_databaseFilePath);
			return JsonConvert.DeserializeObject<List<DdUser>>(dbString);
		}

		public async Task GetDatabaseFileIntoFolder()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_databaseFilePath)!);

				string dbString = await _webService.GetLatestDatabaseString();

				await File.WriteAllTextAsync(_databaseFilePath, dbString);
			}
			catch (Exception ex)
			{
				await _loggingService.LogAsync(new LogMessage(LogSeverity.Critical, "Startup", "Failed to get database file into folder.", ex));
				await Program.StopBot();
			}
		}
	}
}