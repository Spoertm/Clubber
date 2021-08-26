using Clubber.Models;
using Clubber.Models.Responses;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public class WebService
	{
		private const string _getMultipleUsersByIdUrl = "http://l.sorath.com/dd/get_multiple_users_by_id_public.php";
		private const string _getScoresUrl = "http://dd.hasmodai.com/backend15/get_scores.php";
		private readonly SocketTextChannel _databaseBackupChannel;
		private readonly SocketTextChannel _lbEntriesCacheChannel;
		private readonly HttpClient _httpClient;

		public WebService(DiscordSocketClient client)
		{
			_databaseBackupChannel = (client.GetChannel(Constants.DatabaseBackupChannelId) as SocketTextChannel)!;
			_lbEntriesCacheChannel = (client.GetChannel(Constants.LbEntriesCacheChannelId) as SocketTextChannel)!;

			if (_databaseBackupChannel is null || _lbEntriesCacheChannel is null)
				throw new("Database backup- and/or LB entries channels are null.");

			_httpClient = new();
		}

		public async Task<List<LeaderboardUser>> GetLbPlayers(IEnumerable<uint> ids)
		{
			try
			{
				List<KeyValuePair<string?, string?>> postValues = new()
				{
					new("uid", string.Join(',', ids)),
				};

				using FormUrlEncodedContent content = new(postValues);
				HttpResponseMessage response = await _httpClient.PostAsync(_getMultipleUsersByIdUrl, content);
				byte[] data = await response.Content.ReadAsByteArrayAsync();

				int bytePosition = 19;
				List<LeaderboardUser> users = new();
				while (bytePosition < data.Length)
				{
					users.Add(new(
						Username: GetUserName(data, ref bytePosition),
						Rank: BitConverter.ToInt32(data, bytePosition),
						Id: BitConverter.ToInt32(data, bytePosition + 4),
						Time: BitConverter.ToInt32(data, bytePosition + 12),
						Kills: BitConverter.ToInt32(data, bytePosition + 16),
						Gems: BitConverter.ToInt32(data, bytePosition + 28),
						DaggersHit: BitConverter.ToInt32(data, bytePosition + 24),
						DaggersFired: BitConverter.ToInt32(data, bytePosition + 20),
						DeathType: BitConverter.ToInt16(data, bytePosition + 32),
						TimeTotal: BitConverter.ToUInt64(data, bytePosition + 60),
						KillsTotal: BitConverter.ToUInt64(data, bytePosition + 44),
						GemsTotal: BitConverter.ToUInt64(data, bytePosition + 68),
						DeathsTotal: BitConverter.ToUInt64(data, bytePosition + 36),
						DaggersHitTotal: BitConverter.ToUInt64(data, bytePosition + 76),
						DaggersFiredTotal: BitConverter.ToUInt64(data, bytePosition + 52)));

					bytePosition += 88;
				}

				return users;
			}
			catch (Exception e)
			{
				throw new CustomException("DD servers are experiencing issues atm. Try again later.", e);
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

		public async Task BackupDbFile(string filePath, string? text)
			=> await _databaseBackupChannel.SendFileAsync(filePath, text);

		public async Task BackupLbEntriesCache(string filePath, string? text)
			=> await _lbEntriesCacheChannel.SendFileAsync(filePath, text);

		public async Task<string> GetLatestFileContentsFromChannel(ulong channelId)
		{
			SocketTextChannel channelOfChoice = channelId switch
			{
				Constants.DatabaseBackupChannelId => _databaseBackupChannel,
				Constants.LbEntriesCacheChannelId => _lbEntriesCacheChannel,
				_                                 => throw new CustomException($"Faulty argument. Channel ID \"{channelId}\" unsupported."),
			};

			IAttachment? latestAttachment = (await channelOfChoice.GetMessagesAsync(1).FlattenAsync())
				.FirstOrDefault()?
				.Attachments
				.FirstOrDefault();

			if (latestAttachment is null)
				throw new CustomException($"No files in {channelOfChoice.Name} channel.");

			return await _httpClient.GetStringAsync(latestAttachment.Url);
		}

		// Taken from devildaggers.info
		// Credit goes to Noah Stolk https://github.com/NoahStolk
		public async Task<LeaderboardResponse> GetLeaderboardEntries(int rankStart)
		{
			using FormUrlEncodedContent content = new(new[] { new KeyValuePair<string?, string?>("offset", (rankStart - 1).ToString()) });
			using HttpResponseMessage response = await _httpClient.PostAsync(_getScoresUrl, content);

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
				EntryResponse entry = new();

				short usernameLength = br.ReadInt16();
				entry.Username = Encoding.UTF8.GetString(br.ReadBytes(usernameLength));
				entry.Rank = br.ReadInt32();
				entry.Id = br.ReadInt32();
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
	}
}
