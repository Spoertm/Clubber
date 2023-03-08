﻿using Clubber.Domain.Extensions;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text;

namespace Clubber.Domain.BackgroundTasks;

public class DdNewsPostService : AbstractBackgroundService
{
	private const int _minimumScore = 930;

	private readonly IConfiguration _config;
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IDiscordHelper _discordHelper;
	private readonly IWebService _webService;
	private readonly IServiceScopeFactory _services;

	private readonly StringBuilder _sb = new();
	private readonly LeaderboardImageGenerator _imageGenerator = new();

	private SocketTextChannel? _ddNewsChannel;

	public DdNewsPostService(
		IConfiguration config,
		IDatabaseHelper databaseHelper,
		IDiscordHelper discordHelper,
		IWebService webService,
		IServiceScopeFactory services)
	{
		_config = config;
		_databaseHelper = databaseHelper;
		_discordHelper = discordHelper;
		_webService = webService;
		_services = services;
	}

	protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		Log.Debug("Executing {Class}", GetType().Name);

		await _databaseHelper.CleanUpNewsItems();
		_ddNewsChannel ??= _discordHelper.GetTextChannel(_config.GetValue<ulong>("DdNewsChannelId"));
		using IServiceScope scope = _services.CreateScope();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		List<EntryResponse> oldEntries = dbContext.LeaderboardCache.AsNoTracking().ToList();
		List<EntryResponse> newEntries = await _webService.GetSufficientLeaderboardEntries(_minimumScore);

		if (newEntries.Count == 0)
		{
			Log.Information("Fetched zero new leaderboard entries");
			return;
		}

		(EntryResponse, EntryResponse)[] entryTuples = oldEntries.Join(
				inner: newEntries,
				outerKeySelector: oldEntry => oldEntry.Id,
				innerKeySelector: newEntry => newEntry.Id,
				resultSelector: (oldEntry, newEntry) => (oldEntry, newEntry))
			.ToArray();

		bool cacheIsToBeRefreshed = newEntries.Count > oldEntries.Count;
		foreach ((EntryResponse oldEntry, EntryResponse newEntry) in entryTuples)
		{
			if (oldEntry.Time == newEntry.Time)
				continue;

			cacheIsToBeRefreshed = true;
			if (newEntry.Time / 10000 < 1000)
				continue;

			Log.Information("Posting news for player entry {@Player}", newEntry);

			int nth = newEntries.Count(entry => entry.Time / 1000000 >= newEntry.Time / 1000000);

			Log.Debug("Getting DD News message");
			string message = GetDdNewsMessage(oldEntry, newEntry, nth);

			Log.Debug("Getting country code");
			string? countryCode = await GetCountryCode(newEntry);

			Log.Debug("Getting DD News screenshot");
			using (MemoryStream screenshot = _imageGenerator.CreateImage(newEntry.Rank, newEntry.Username, newEntry.Time, countryCode))
			{
				Log.Debug("Sending DD News to Discord");
				await _ddNewsChannel.SendFileAsync(screenshot, $"{newEntry.Username}_{newEntry.Time}.png", message);
			}

			Log.Debug("Adding news item to database");
			await _databaseHelper.AddDdNewsItem(oldEntry, newEntry, nth);

			async Task<string?> GetCountryCode(EntryResponse entry)
			{
				try
				{
					return await _webService.GetCountryCodeForplayer(entry.Id);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to fetch country code for EntryResponse {@Player}", entry);
					return null;
				}
			}
		}

		if (cacheIsToBeRefreshed)
		{
			Log.Information("Updating leaderboard cache");
			await _databaseHelper.UpdateLeaderboardCache(newEntries);
		}
		else
		{
			Log.Debug("No DD News posting required");
		}

		_sb.Clear();
	}

	private string GetDdNewsMessage(EntryResponse oldEntry, EntryResponse newEntry, int nth)
	{
		string userName = newEntry.Username;
		ulong ddPalsId = _config.GetValue<ulong>("DdPalsId");
		if (_databaseHelper.GetDdUserBy(newEntry.Id) is { } dbUser && _discordHelper.GetGuildUser(ddPalsId, dbUser.DiscordId) is { } guildUser)
			userName = guildUser.Mention;

		double oldScore = oldEntry.Time / 10000d;
		double newScore = newEntry.Time / 10000d;
		int ranksChanged = oldEntry.Rank - newEntry.Rank;
		_sb.Clear()
			.Append("Congratulations to ")
			.Append(userName)
			.Append(" for getting a new PB of ")
			.Append($"{newScore:0.0000}")
			.Append(" seconds! They beat their old PB of ")
			.Append($"{oldScore:0.0000}")
			.Append("s (**+")
			.Append($"{newScore - oldScore:0.0000}")
			.Append("s**), ")
			.Append(ranksChanged > 0 ? "gaining " : ranksChanged == 0 ? "but didn't change" : "but lost ")
			.Append(ranksChanged == 0 ? "" : Math.Abs(ranksChanged))
			.Append(Math.Abs(ranksChanged) is 1 or 0 ? " rank." : " ranks.");

		int oldHundredth = oldEntry.Time / 1000000;
		int newHundredth = newEntry.Time / 1000000;
		if (newHundredth > oldHundredth)
		{
			_sb.Append(" They are the ")
				.Append(nth)
				.Append(nth.OrdinalIndicator());

			if (oldHundredth == 9 && newHundredth == 10)
				_sb.Append(" player to unlock the leviathan dagger!");
			else
				_sb.Append($" {newHundredth * 100} player!");
		}

		if (newEntry.Rank == 1)
			_sb.Append(Format.Bold(" It's a new WR! 👑 🎉"));

		return _sb.ToString();
	}
}
