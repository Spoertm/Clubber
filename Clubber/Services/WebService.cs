using Clubber.Database;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public class WebService
	{
		private const string _getMultipleUsersByIdUrl = "http://l.sorath.com/dd/get_multiple_users_by_id_public.php";
		private readonly HttpClient _httpClient;
		private readonly SocketTextChannel _backupChannel;

		public WebService(DiscordSocketClient client)
		{
			_backupChannel = (client.GetChannel(Constants.DatabaseBackupChannelId) as SocketTextChannel)!;
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
					users.Add(new LeaderboardUser(
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
		{
			await _backupChannel.SendFileAsync(filePath, text);
		}

		public async Task<string> GetLatestDatabaseString()
		{
			IAttachment? latestAttachment = (await _backupChannel.GetMessagesAsync(1).FlattenAsync())
				.FirstOrDefault()?
				.Attachments
				.First();

			if (latestAttachment is null)
				throw new CustomException("No files in backup channel.");

			return await _httpClient.GetStringAsync(latestAttachment.Url);
		}
	}
}
