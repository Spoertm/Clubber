using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Serilog;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace Clubber.Domain.Services;

public sealed class WebService(IHttpClientFactory httpClientFactory) : IWebService
{
	private readonly Uri _getMultipleUsersByIdUri = new("http://dd.hasmodai.com/dd3/get_multiple_users_by_id_public.php");
	private readonly Uri _getScoresUri = new("http://dd.hasmodai.com/dd3/get_scores.php");
	private readonly Uri _getWorldRecordsUri = new("https://devildaggers.info/api/world-records");

	private readonly JsonSerializerOptions _serializerOptions = new()
	{
		PropertyNameCaseInsensitive = true,
	};

	public async Task<IReadOnlyList<EntryResponse>> GetLbPlayers(IEnumerable<uint> ids)
	{
		try
		{
			List<KeyValuePair<string?, string?>> postValues =
			[
				new("uid", string.Join(',', ids)),
			];

			using FormUrlEncodedContent content = new(postValues);
			using HttpClient client = httpClientFactory.CreateClient();
			using HttpResponseMessage response = await client.PostAsync(_getMultipleUsersByIdUri, content);
			byte[] data = await response.Content.ReadAsByteArrayAsync();

			int bytePosition = 19;
			List<EntryResponse> users = [];
			while (bytePosition < data.Length)
			{
				users.Add(new EntryResponse
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
			Log.Error(e, "{Class}.GetLbPlayers => Failed to fetch leaderboard players", nameof(WebService));
			throw new ClubberException("DD servers are experiencing issues atm.", e);
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
				Log.Error(e, "{Class}.GetSufficientLeaderboardEntries => failed to fetch LB entries", nameof(WebService));
				throw;
			}

			rank += 100;
			await Task.Delay(2000);
		} while (entries[^1].Time / 10_000 >= minimumScore);

		return entries;
	}

	// Taken from devildaggers.info then modified
	// Credit goes to Noah Stolk https://github.com/NoahStolk
	private async Task<LeaderboardResponse> GetLeaderboardEntries(int rankStart)
	{
		using FormUrlEncodedContent content = new([new KeyValuePair<string, string>("offset", (rankStart - 1).ToString())]);
		using HttpClient client = httpClientFactory.CreateClient();
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
		using HttpClient client = httpClientFactory.CreateClient();
		await using Stream responseStream = await client.GetStreamAsync(uri);
		using JsonDocument jsonDocument = await JsonDocument.ParseAsync(responseStream);

		return jsonDocument.RootElement.TryGetProperty("countryCode", out JsonElement countryCodeElement) ? countryCodeElement.GetString() : null;
	}

	public async Task<GetPlayerHistory?> GetPlayerHistory(uint lbId)
	{
		Uri uri = new($"https://devildaggers.info/api/clubber/players/{lbId}/history");
		using HttpClient client = httpClientFactory.CreateClient();
		await using Stream responseStream = await client.GetStreamAsync(uri);
		return await JsonSerializer.DeserializeAsync<GetPlayerHistory>(responseStream, _serializerOptions);
	}

	public async Task<DdStatsFullRunResponse> GetDdstatsResponse(Uri uri)
	{
		string uriStr = uri.ToString();
		string runIdStr = string.Empty;
		if (uriStr.StartsWith("https://ddstats.com/games/", StringComparison.OrdinalIgnoreCase))
			runIdStr = uriStr[26..];
		else if (uriStr.StartsWith("https://www.ddstats.com/games/", StringComparison.OrdinalIgnoreCase))
			runIdStr = uriStr[30..];
		else if (uriStr.StartsWith("https://ddstats.com/api/v2/game/full", StringComparison.OrdinalIgnoreCase))
			runIdStr = uriStr[40..];
		else if (uriStr.StartsWith("https://www.ddstats.com/api/v2/game/full", StringComparison.OrdinalIgnoreCase))
			runIdStr = uriStr[44..];

		bool successfulParse = uint.TryParse(runIdStr, out uint runId);
		if (string.IsNullOrEmpty(runIdStr) || !successfulParse)
			throw new ClubberException("Invalid ddstats URL.");

		Uri fullRunReqUri = new($"https://ddstats.com/api/v2/game/full?id={runId}");
		using HttpClient client = httpClientFactory.CreateClient();
		await using Stream ddstatsResponseStream = await client.GetStreamAsync(fullRunReqUri);
		return await JsonSerializer.DeserializeAsync<DdStatsFullRunResponse>(ddstatsResponseStream) ?? throw new SerializationException();
	}

	public async Task<GetWorldRecordDataContainer> GetWorldRecords()
	{
		using HttpClient client = httpClientFactory.CreateClient();
		await using Stream responseStream = await client.GetStreamAsync(_getWorldRecordsUri);
		return await JsonSerializer.DeserializeAsync<GetWorldRecordDataContainer>(responseStream, _serializerOptions)
		       ?? throw new SerializationException("Failed to deserialize world records data");
	}
}
