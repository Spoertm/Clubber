using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Extensions;
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

namespace Clubber.Discord;

public class DdNewsPostService : RepeatingBackgroundService
{
	private const int _minimumScore = 930;

	private readonly IConfiguration _config;
	private readonly IServiceScopeFactory _services;

	private readonly StringBuilder _sb = new();
	private readonly LeaderboardImageGenerator _imageGenerator = new();

	private SocketTextChannel? _ddNewsChannel;

	public DdNewsPostService(
		IConfiguration config,
		IServiceScopeFactory services)
	{
		_config = config;
		_services = services;
	}

	protected override TimeSpan TickInterval => TimeSpan.FromMinutes(1);

	protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
	{
		Log.Debug("Executing {Class}", GetType().Name);

		await using AsyncServiceScope scope = _services.CreateAsyncScope();
		IDatabaseHelper databaseHelper = scope.ServiceProvider.GetRequiredService<IDatabaseHelper>();
		IDiscordHelper discordHelper = scope.ServiceProvider.GetRequiredService<IDiscordHelper>();
		await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
		IWebService webService = scope.ServiceProvider.GetRequiredService<IWebService>();

		await databaseHelper.CleanUpNewsItems();
		_ddNewsChannel ??= discordHelper.GetTextChannel(_config.GetValue<ulong>("DdNewsChannelId"));
		EntryResponse[] oldEntries = await dbContext.LeaderboardCache.AsNoTracking().ToArrayAsync(stoppingToken);
		ICollection<EntryResponse> newEntries = await webService.GetSufficientLeaderboardEntries(_minimumScore);

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

		bool cacheIsToBeRefreshed = newEntries.Count > oldEntries.Length;
		foreach ((EntryResponse oldEntry, EntryResponse newEntry) in entryTuples)
		{
			if (oldEntry.Time == newEntry.Time)
				continue;

			cacheIsToBeRefreshed = true;
			if (newEntry.Time / 10_000 < 1000)
				continue;

			Log.Information("Posting news for player entry {@Player}", newEntry);

			int nth = newEntries.Count(entry => entry.Time / 1_000_000 >= newEntry.Time / 1_000_000);

			Log.Debug("Getting DD News message");
			string message = await GetDdNewsMessage(databaseHelper, discordHelper, oldEntry, newEntry, nth);

			Log.Debug("Getting country code");
			string? countryCode = await GetCountryCode(newEntry);

			Log.Debug("Getting DD News screenshot");
			using (MemoryStream screenshot = _imageGenerator.CreateImage(newEntry.Rank, newEntry.Username, newEntry.Time, countryCode))
			{
				Log.Debug("Sending DD News to Discord");
				await _ddNewsChannel.SendFileAsync(screenshot, $"{newEntry.Username}_{newEntry.Time}.png", message);
			}

			Log.Debug("Adding news item to database");
			await databaseHelper.AddDdNewsItem(oldEntry, newEntry, nth);

			async Task<string?> GetCountryCode(EntryResponse entry)
			{
				try
				{
					return await webService.GetCountryCodeForplayer(entry.Id);
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
			await databaseHelper.UpdateLeaderboardCache(newEntries);
		}
		else
		{
			Log.Debug("No DD News posting required");
		}

		_sb.Clear();
	}

	private async Task<string> GetDdNewsMessage(
		IDatabaseHelper databaseHelper,
		IDiscordHelper discordHelper,
		EntryResponse oldEntry,
		EntryResponse newEntry,
		int nth)
	{
		string userName = newEntry.Username;
		ulong ddPalsId = _config.GetValue<ulong>("DdPalsId");
		if (await databaseHelper.GetDdUserBy(newEntry.Id) is { } dbUser && discordHelper.GetGuildUser(ddPalsId, dbUser.DiscordId) is { } guildUser)
			userName = guildUser.Mention;

		double oldScore = oldEntry.Time / 10_000d;
		double newScore = newEntry.Time / 10_000d;
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

		int oldHundredth = oldEntry.Time / 1_000_000;
		int newHundredth = newEntry.Time / 1_000_000;
		if (newHundredth > oldHundredth)
		{
			_sb.Append(" They are the ")
				.Append(nth)
				.Append(nth.OrdinalNumeral());

			if (oldHundredth < 10 && newHundredth == 10)
				_sb.Append(" player to unlock the leviathan dagger!");
			else
				_sb.Append($" {newHundredth * 100} player!");
		}

		if (newEntry.Rank == 1)
			_sb.Append(Format.Bold(" It's a new WR! 👑 🎉"));

		return _sb.ToString();
	}
}
