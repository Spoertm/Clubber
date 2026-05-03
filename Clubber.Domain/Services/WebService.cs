using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Clubber.Domain.Configuration;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Microsoft.Extensions.Options;
using Serilog;

namespace Clubber.Domain.Services;

public sealed class WebService(IHttpClientFactory httpClientFactory, IOptions<AppConfig> appConfig) : IWebService
{
    private readonly AppConfig _appConfig = appConfig.Value;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
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
            using HttpResponseMessage response = await client.PostAsync(_appConfig.Endpoints.GetMultipleUsersById, content);
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
                    Id = BitConverter.ToUInt32(data, bytePosition + 4),
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

    public async Task<string?> GetCountryCodeForplayer(uint lbId)
    {
        try
        {
            Uri uri = new($"{_appConfig.Endpoints.GetCountryCodeForPlayer}{lbId}/country-code");
            using HttpClient client = httpClientFactory.CreateClient();
            await using Stream responseStream = await client.GetStreamAsync(uri);
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
            Uri uri = new($"{_appConfig.Endpoints.GetPlayerHistory}{lbId}/history");
            using HttpClient client = httpClientFactory.CreateClient();
            await using Stream responseStream = await client.GetStreamAsync(uri);
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
        Uri fullRunReqUri = new($"{_appConfig.Endpoints.GetDdstatsResponse}?id={runId}");
        using HttpClient client = httpClientFactory.CreateClient();
        await using Stream ddstatsResponseStream = await client.GetStreamAsync(fullRunReqUri);
        return await JsonSerializer.DeserializeAsync<DdStatsFullRunResponse>(ddstatsResponseStream) ?? throw new SerializationException();
    }

    public async Task<GetWorldRecordDataContainer> GetWorldRecords()
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient();
            await using Stream responseStream = await client.GetStreamAsync(_appConfig.Endpoints.GetWorldRecords);
            return await JsonSerializer.DeserializeAsync<GetWorldRecordDataContainer>(responseStream, _serializerOptions)
                   ?? throw new SerializationException("Failed to deserialize world records data");
        }
        catch (Exception e)
        {
            Log.Error(e, "{Class}.GetWorldRecords => Failed to fetch world records", nameof(WebService));
            throw new ClubberException("Failed to fetch world records data.", e);
        }
    }

    public async Task<IReadOnlyList<GetRecentResponse>> GetRecentScores(DateTimeOffset before, int limit)
    {
        try
        {
            long unixTime = before.ToUnixTimeSeconds();
            Uri uri = new($"{_appConfig.Endpoints.GetRecentScores}?before={unixTime}&limit={limit}");
            using HttpClient client = httpClientFactory.CreateClient();
            string responseString = await client.GetStringAsync(uri);

            if (string.IsNullOrWhiteSpace(responseString))
            {
                return [];
            }

            // API returns concatenated JSON objects: {...}{...}
            // Wrap them in a JSON array so we can deserialize normally.
            string jsonArray = '[' + responseString.Replace("}{", "},{", StringComparison.Ordinal) + ']';
            return JsonSerializer.Deserialize<List<GetRecentResponse>>(jsonArray, _serializerOptions)
                   ?? throw new SerializationException("Failed to deserialize recent scores data");
        }
        catch (Exception e)
        {
            Log.Error(e, "{Class}.GetRecentScores => Failed to fetch recent scores", nameof(WebService));
            throw new ClubberException("Failed to fetch recent scores data.", e);
        }
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

    private static string GetUserName(byte[] data, ref int bytePos)
    {
        short usernameLength = BitConverter.ToInt16(data, bytePos);
        bytePos += 2;

        byte[] usernameBytes = new byte[usernameLength];
        Buffer.BlockCopy(data, bytePos, usernameBytes, 0, usernameLength);

        bytePos += usernameLength;
        return Encoding.UTF8.GetString(usernameBytes);
    }
}
