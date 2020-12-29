using Clubber.Databases;
using Clubber.Files;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class UpdateRolesHelper
	{
		private readonly DiscordSocketClient _client;
		private readonly ScoreRoles _scoreRoles;
		private readonly DatabaseHelper _databaseHelper;

		private readonly SocketGuild _guild;

		public UpdateRolesHelper(DiscordSocketClient client, ScoreRoles scoreRoles, DatabaseHelper databaseHelper)
		{
			_client = client;
			_scoreRoles = scoreRoles;
			_databaseHelper = databaseHelper;

			_guild = _client.GetGuild(399568958669455364);
		}

		public record UpdateRolesResponse(int UpdatedUsers, int NonMemberCount, UpdateRoleResponse[] UpdateResponses);

		public async Task<UpdateRolesResponse> UpdateRolesAndDb()
		{
			List<DdUser> dbUsersList = _databaseHelper.GetUsers();

			int nonMemberCount = dbUsersList.Count(dbUser => _guild.GetUser(dbUser.DiscordId) == null);

			List<Task<UpdateRoleResponse>> tasks = new();
			foreach (DdUser user in dbUsersList)
				tasks.Add(UpdateUserRoles(user));

			UpdateRoleResponse[] responses = await Task.WhenAll(tasks);
			int usersUpdated = responses.Count(b => b.Success);
			return new(usersUpdated, nonMemberCount, responses);
		}

		public record UpdateRoleResponse(bool Success, bool MemberHasRole, SocketRole RoleToAdd, List<SocketRole> RemovedRoles);

		public async Task<UpdateRoleResponse> UpdateUserRoles(DdUser user)
		{
			SocketGuildUser guildMember = _guild.GetUser(user.DiscordId);
			if (guildMember == null)
				return new(false, false, null, null);

			int newScore = await GetUserTimeFromHasmodai(user.LeaderboardId) / 10000;
			_databaseHelper.FindAndUpdateUser(guildMember.Id, newScore);

			KeyValuePair<int, ulong> scoreRole = _scoreRoles.ScoreRoleDictionary.Where(sr => sr.Key <= newScore).OrderByDescending(sr => sr.Key).FirstOrDefault();
			SocketRole roleToAdd = _guild.GetRole(scoreRole.Value);
			List<SocketRole> removedRoles = await RemoveScoreRolesExcept(guildMember, roleToAdd);

			bool memberHasRole = MemberHasRole(guildMember, roleToAdd.Id);
			if (removedRoles.Count == 0 && memberHasRole)
				return new(false, memberHasRole, roleToAdd, removedRoles);

			return new(true, memberHasRole, roleToAdd, removedRoles);
		}

		private static async Task<int> GetUserTimeFromHasmodai(int userId)
		{
			Dictionary<string, string> postValues = new Dictionary<string, string> { { "uid", userId.ToString() } };

			using FormUrlEncodedContent content = new FormUrlEncodedContent(postValues);
			using HttpClient client = new HttpClient();
			HttpResponseMessage resp = await client.PostAsync("http://dd.hasmodai.com/backend16/get_user_by_id_public.php", content);
			byte[] data = await resp.Content.ReadAsByteArrayAsync();

			int bytePos = 19;
			short usernameLength = BitConverter.ToInt16(data, bytePos);
			bytePos += usernameLength + sizeof(short);
			return BitConverter.ToInt32(data, bytePos + 12);
		}

		public async Task<List<SocketRole>> RemoveScoreRolesExcept(SocketGuildUser member, SocketRole excludedRole)
		{
			List<SocketRole> removedRoles = new List<SocketRole>();

			const ulong newMemberRoleId = 728663492424499200;
			foreach (SocketRole role in member.Roles)
			{
				if (_scoreRoles.ScoreRoleDictionary.ContainsValue(role.Id) && role.Id != excludedRole.Id || role.Id == newMemberRoleId)
				{
					await member.RemoveRoleAsync(role);
					removedRoles.Add(role);
				}
			}

			return removedRoles;
		}

		private static bool MemberHasRole(SocketGuildUser member, ulong roleId)
			=> member.Roles.Any(role => role.Id == roleId);
	}
}
