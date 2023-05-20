using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace Clubber.Domain.Services;

public class WebService : IWebService
{
#pragma warning disable S1075
	private const string _getMultipleUsersByIdUrl = "http://l.sorath.com/dd/get_multiple_users_by_id_public.php";
	private const string _getScoresUrl = "http://dd.hasmodai.com/backend15/get_scores.php";
#pragma warning restore S1075
	private readonly IHttpClientFactory _httpClientFactory;

	public WebService(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

	public async Task<List<EntryResponse>> GetLbPlayers(IEnumerable<uint> ids)
	{
		try
		{
			List<KeyValuePair<string?, string?>> postValues = new()
			{
				new("uid", string.Join(',', ids)),
			};

			using FormUrlEncodedContent content = new(postValues);
			HttpResponseMessage response = await _httpClientFactory.CreateClient().PostAsync(_getMultipleUsersByIdUrl, content);
			byte[] data = await response.Content.ReadAsByteArrayAsync();

			int bytePosition = 19;
			List<EntryResponse> users = new();
			while (bytePosition < data.Length)
			{
				users.Add(new()
				{
					Username = GetUserName(data, ref bytePosition),
					Rank = BitConverter.ToInt32(data, bytePosition),
					Id = BitConverter.ToInt32(data, bytePosition + 4),
					Time = BitConverter.ToInt32(data, bytePosition + 12),
					Kills = BitConverter.ToInt32(data, bytePosition + 16),
					Gems = BitConverter.ToInt32(data, bytePosition + 28),
					DaggersHit = BitConverter.ToInt32(data, bytePosition + 24),
					DaggersFired = BitConverter.ToInt32(data, bytePosition + 20),
					DeathType = BitConverter.ToInt32(data, bytePosition + 32),
					TimeTotal = BitConverter.ToUInt64(data, bytePosition + 60),
					KillsTotal = BitConverter.ToUInt64(data, bytePosition + 44),
					GemsTotal = BitConverter.ToUInt64(data, bytePosition + 68),
					DeathsTotal = BitConverter.ToUInt64(data, bytePosition + 36),
					DaggersHitTotal = BitConverter.ToUInt64(data, bytePosition + 76),
					DaggersFiredTotal = BitConverter.ToUInt64(data, bytePosition + 52),
				});

				bytePosition += 88;
			}

			return users;
		}
		catch (Exception e)
		{
			Log.Error(e, "{Class}.GetLbPlayers => Failed to fetch leaderboard players", GetType().Name);
			throw new ClubberException("DD servers are experiencing issues atm. Try again later.", e);
		}
	}

	private string GetUserName(byte[] data, ref int bytePos)
	{
		short usernameLength = BitConverter.ToInt16(data, bytePos);
		bytePos += 2;

		byte[] usernameBytes = new byte[usernameLength];
		Buffer.BlockCopy(data, bytePos, usernameBytes, 0, usernameLength);

		bytePos += usernameLength;
		return Encoding.UTF8.GetString(usernameBytes);
	}

	public async Task<List<EntryResponse>> GetSufficientLeaderboardEntries(int minimumScore)
	{
		List<EntryResponse> entries = new();
		int rank = 1;
		do
		{
			try
			{
				entries.AddRange((await GetLeaderboardEntries(rank)).Entries);
			}
			catch (Exception e)
			{
				Log.Error(e, "{Class}.GetSufficientLeaderboardEntries => failed to fetch LB entries", GetType().Name);
				throw;
			}

			rank += 100;
			await Task.Delay(2000);
		}
		while (entries[^1].Time / 10000 >= minimumScore);

		return entries;
	}

	// Taken from devildaggers.info then modified
	// Credit goes to Noah Stolk https://github.com/NoahStolk
	private async Task<LeaderboardResponse> GetLeaderboardEntries(int rankStart)
	{
		using FormUrlEncodedContent content = new(new[] { new KeyValuePair<string?, string?>("offset", (rankStart - 1).ToString()) });
		using HttpResponseMessage response = await _httpClientFactory.CreateClient().PostAsync(_getScoresUrl, content);

		MemoryStream ms = new();
		await response.Content.CopyToAsync(ms);
		using BinaryReader br = new(ms);

		LeaderboardResponse leaderboard = new()
		{
			DateTime = DateTime.UtcNow,
		};

		br.BaseStream.Seek(11, SeekOrigin.Begin);
		leaderboard.DeathsGlobal = br.ReadUInt64();
		leaderboard.KillsGlobal = br.ReadUInt64();
		leaderboard.DaggersFiredGlobal = br.ReadUInt64();
		leaderboard.TimeGlobal = br.ReadUInt64();
		leaderboard.GemsGlobal = br.ReadUInt64();
		leaderboard.DaggersHitGlobal = br.ReadUInt64();
		leaderboard.TotalEntries = br.ReadUInt16();

		br.BaseStream.Seek(14, SeekOrigin.Current);
		leaderboard.TotalPlayers = br.ReadInt32();

		br.BaseStream.Seek(4, SeekOrigin.Current);
		for (int i = 0; i < leaderboard.TotalEntries; i++)
		{
			short usernameLength = br.ReadInt16();
			EntryResponse entry = new()
			{
				Username = Encoding.UTF8.GetString(br.ReadBytes(usernameLength)),
				Rank = br.ReadInt32(),
				Id = br.ReadInt32(),
				Time = br.ReadInt32(),
				Kills = br.ReadInt32(),
				DaggersFired = br.ReadInt32(),
				DaggersHit = br.ReadInt32(),
				Gems = br.ReadInt32(),
				DeathType = br.ReadInt32(),
				DeathsTotal = br.ReadUInt64(),
				KillsTotal = br.ReadUInt64(),
				DaggersFiredTotal = br.ReadUInt64(),
				TimeTotal = br.ReadUInt64(),
				GemsTotal = br.ReadUInt64(),
				DaggersHitTotal = br.ReadUInt64(),
			};

			br.BaseStream.Seek(4, SeekOrigin.Current);

			leaderboard.Entries.Add(entry);
		}

		return leaderboard;
	}

	public async Task<string?> GetCountryCodeForplayer(int lbId)
	{
		string url = $"https://devildaggers.info/api/clubber/players/{lbId}/country-code";
		string responseStr = await _httpClientFactory.CreateClient().GetStringAsync(url);
		return JsonConvert.DeserializeObject<dynamic>(responseStr)?.countryCode;
	}

	public async Task<DateTime?> GetPlayerPbDateTime(int leaderboardId)
	{
		string url = $"https://devildaggers.info/api/players/{leaderboardId}/history";
		string responseStr = await _httpClientFactory.CreateClient().GetStringAsync(url);
		GetPlayerHistory? playerHistory = JsonConvert.DeserializeObject<GetPlayerHistory>(responseStr);
		return playerHistory?.ScoreHistory.LastOrDefault()?.DateTime;
	}

	public async Task<DdStatsFullRunResponse> GetDdstatsResponse(string url)
	{
		if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
			throw new ClubberException("Invalid URL");

		string runIdStr = string.Empty;
		if (url.StartsWith("https://ddstats.com/games/"))
			runIdStr = url[26..];
		else if (url.StartsWith("https://www.ddstats.com/games/"))
			runIdStr = url[30..];
		else if (url.StartsWith("https://ddstats.com/api/v2/game/full"))
			runIdStr = url[40..];
		else if (url.StartsWith("https://www.ddstats.com/api/v2/game/full"))
			runIdStr = url[44..];

		bool successfulParse = uint.TryParse(runIdStr, out uint runId);
		if (string.IsNullOrEmpty(runIdStr) || !successfulParse)
			throw new ClubberException("Invalid ddstats URL.");

		string fullRunReqUrl = $"https://ddstats.com/api/v2/game/full?id={runId}";
		string ddstatsResponseStr = await _httpClientFactory.CreateClient().GetStringAsync(fullRunReqUrl);
		return JsonConvert.DeserializeObject<DdStatsFullRunResponse>(ddstatsResponseStr) ?? throw new JsonSerializationException();
	}
}
