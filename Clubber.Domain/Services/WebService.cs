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
			response.EnsureSuccessStatusCode();
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
				Log.Error(e, "{Class}.GetSufficientLeaderboardEntries => Failed to fetch leaderboard entries", nameof(WebService));
				throw new ClubberException("DD servers are experiencing issues atm.", e);
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
		response.EnsureSuccessStatusCode();

		using BinaryReader br = new(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));

		LeaderboardResponse leaderboard = new() { DateTime = DateTime.UtcNow };

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
			};

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
		try
		{
			Uri uri = new($"https://devildaggers.info/api/clubber/players/{lbId}/country-code");
			using HttpClient client = httpClientFactory.CreateClient();
			using HttpResponseMessage response = await client.GetAsync(uri);
			response.EnsureSuccessStatusCode();
			await using Stream responseStream = await response.Content.ReadAsStreamAsync();
			using JsonDocument jsonDocument = await JsonDocument.ParseAsync(responseStream);

			return jsonDocument.RootElement.TryGetProperty("countryCode", out JsonElement countryCodeElement) ? countryCodeElement.GetString() : null;
		}
		catch (Exception e)
		{
			Log.Error(e, "{Class}.GetCountryCodeForplayer => Failed to fetch country code for {LeaderboardId}", nameof(WebService), lbId);
			throw new ClubberException("Failed to fetch country code data.", e);
		}
	}

	public async Task<GetPlayerHistory?> GetPlayerHistory(uint lbId)
	{
		try
		{
			Uri uri = new($"https://devildaggers.info/api/clubber/players/{lbId}/history");
			using HttpClient client = httpClientFactory.CreateClient();
			using HttpResponseMessage response = await client.GetAsync(uri);
			response.EnsureSuccessStatusCode();
			await using Stream responseStream = await response.Content.ReadAsStreamAsync();
			return await JsonSerializer.DeserializeAsync<GetPlayerHistory>(responseStream, _serializerOptions);
		}
		catch (Exception e)
		{
			Log.Error(e, "{Class}.GetPlayerHistory => Failed to fetch player history for {LeaderboardId}", nameof(WebService), lbId);
			throw new ClubberException("Failed to fetch player history data.", e);
		}
	}

	public async Task<DdStatsFullRunResponse> GetDdstatsResponse(Uri uri)
	{
		uint? runId = ExtractRunIdFromUri(uri) ?? throw new ClubberException("Invalid ddstats URL.");
		Uri fullRunReqUri = new($"https://ddstats.com/api/v2/game/full?id={runId}");
		using HttpClient client = httpClientFactory.CreateClient();
		using HttpResponseMessage response = await client.GetAsync(fullRunReqUri);
		response.EnsureSuccessStatusCode();
		await using Stream ddstatsResponseStream = await response.Content.ReadAsStreamAsync();
		return await JsonSerializer.DeserializeAsync<DdStatsFullRunResponse>(ddstatsResponseStream) ?? throw new SerializationException();
	}

	private static uint? ExtractRunIdFromUri(Uri uri)
	{
		// Handle /api/v2/game/full?id={runId} format
		if (uri.AbsolutePath.StartsWith("/api/v2/game/full", StringComparison.OrdinalIgnoreCase))
		{
			string? query = uri.Query;
			if (query.StartsWith("?id=", StringComparison.OrdinalIgnoreCase) &&
				uint.TryParse(query.AsSpan(4), out uint apiRunId))
			{
				return apiRunId;
			}
		}

		// Handle /games/{runId} format
		if (uri.AbsolutePath.StartsWith("/games/", StringComparison.OrdinalIgnoreCase))
		{
			string lastSegment = uri.Segments[^1];
			if (uint.TryParse(lastSegment, out uint gameRunId))
			{
				return gameRunId;
			}
		}

		return null;
	}

	public async Task<GetWorldRecordDataContainer> GetWorldRecords()
	{
		try
		{
			using HttpClient client = httpClientFactory.CreateClient();
			using HttpResponseMessage response = await client.GetAsync(_getWorldRecordsUri);
			response.EnsureSuccessStatusCode();
			await using Stream responseStream = await response.Content.ReadAsStreamAsync();
			return await JsonSerializer.DeserializeAsync<GetWorldRecordDataContainer>(responseStream, _serializerOptions)
				   ?? throw new SerializationException("Failed to deserialize world records data");
		}
		catch (Exception e)
		{
			Log.Error(e, "{Class}.GetWorldRecords => Failed to fetch world records", nameof(WebService));
			throw new ClubberException("Failed to fetch world records data.", e);
		}
	}
}
