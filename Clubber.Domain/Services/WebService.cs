using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace Clubber.Domain.Services;

public class WebService : IWebService
{
	private readonly Uri _getMultipleUsersByIdUri = new("http://dd.hasmodai.com/dd3/get_multiple_users_by_id_public.php");
	private readonly Uri _getScoresUri = new("http://dd.hasmodai.com/dd3/get_scores.php");
	private readonly IHttpClientFactory _httpClientFactory;

	public WebService(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

	public async Task<IReadOnlyList<EntryResponse>> GetLbPlayers(IEnumerable<uint> ids)
	{
		try
		{
			List<KeyValuePair<string?, string?>> postValues =
			[
				new("uid", string.Join(',', ids)),
			];

			using FormUrlEncodedContent content = new(postValues);
			using HttpClient client = _httpClientFactory.CreateClient();
			using HttpResponseMessage response = await client.PostAsync(_getMultipleUsersByIdUri, content);
			byte[] data = await response.Content.ReadAsByteArrayAsync();

			int bytePosition = 19;
			List<EntryResponse> users = [];
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

	private static string GetUserName(byte[] data, ref int bytePos)
	{
		short usernameLength = BitConverter.ToInt16(data, bytePos);
		bytePos += 2;

		byte[] usernameBytes = new byte[usernameLength];
		Buffer.BlockCopy(data, bytePos, usernameBytes, 0, usernameLength);

		bytePos += usernameLength;
		return Encoding.UTF8.GetString(usernameBytes);
	}

	public async Task<ICollection<EntryResponse>> GetSufficientLeaderboardEntries(int minimumScore)
	{
		List<EntryResponse> entries = [];
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
		while (entries[^1].Time / 10_000 >= minimumScore);

		return entries;
	}

	// Taken from devildaggers.info then modified
	// Credit goes to Noah Stolk https://github.com/NoahStolk
	private async Task<LeaderboardResponse> GetLeaderboardEntries(int rankStart)
	{
		using FormUrlEncodedContent content = new(new[] { new KeyValuePair<string?, string?>("offset", (rankStart - 1).ToString()) });
		using HttpClient client = _httpClientFactory.CreateClient();
		using HttpResponseMessage response = await client.PostAsync(_getScoresUri, content);

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
			EntryResponse entry = new();
			entry.Username = Encoding.UTF8.GetString(br.ReadBytes(usernameLength));
			entry.Rank = br.ReadInt32();
			entry.Id = br.ReadInt32();
			_ = br.ReadInt32();
			entry.Time = br.ReadInt32();
			entry.Kills = br.ReadInt32();
			entry.DaggersFired = br.ReadInt32();
			entry.DaggersHit = br.ReadInt32();
			entry.Gems = br.ReadInt32();
			entry.DeathType = br.ReadInt32();
			entry.DeathsTotal = br.ReadUInt64();
			entry.KillsTotal = br.ReadUInt64();
			entry.DaggersFiredTotal = br.ReadUInt64();
			entry.TimeTotal = br.ReadUInt64();
			entry.GemsTotal = br.ReadUInt64();
			entry.DaggersHitTotal = br.ReadUInt64();

			br.BaseStream.Seek(4, SeekOrigin.Current);

			leaderboard.Entries.Add(entry);
		}

		return leaderboard;
	}

	public async Task<string?> GetCountryCodeForplayer(int lbId)
	{
		Uri uri = new($"https://devildaggers.info/api/clubber/players/{lbId}/country-code");
		using HttpClient client = _httpClientFactory.CreateClient();
		string responseStr = await client.GetStringAsync(uri);
		return JsonConvert.DeserializeObject<dynamic>(responseStr)?.countryCode;
	}

	public async Task<GetPlayerHistory?> GetPlayerHistory(int lbId)
	{
		Uri uri = new($"https://devildaggers.info/api/clubber/players/{lbId}/history");
		using HttpClient client = _httpClientFactory.CreateClient();
		string responseStr = await client.GetStringAsync(uri);
		return JsonConvert.DeserializeObject<GetPlayerHistory>(responseStr);
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
		using HttpClient client = _httpClientFactory.CreateClient();
		string ddstatsResponseStr = await client.GetStringAsync(fullRunReqUrl);
		return JsonConvert.DeserializeObject<DdStatsFullRunResponse>(ddstatsResponseStr) ?? throw new JsonSerializationException();
	}
}
