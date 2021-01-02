using Clubber.Databases;
using Clubber.Files;
using Discord.WebSocket;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class UpdateRolesHelper
	{
		private const ulong _wrRoleId = 446688666325090310;
		private const ulong _top3RoleId = 472451008342261820;
		private const ulong _top10RoleId = 556255819323277312;

		private readonly ScoreRoles _scoreRoles;
		private readonly DatabaseHelper _databaseHelper;
		private readonly SocketGuild _guild;

		public UpdateRolesHelper(DiscordSocketClient client, ScoreRoles scoreRoles, DatabaseHelper databaseHelper)
		{
			_scoreRoles = scoreRoles;
			_databaseHelper = databaseHelper;
			_guild = client.GetGuild(399568958669455364);
		}

		public record UpdateRolesResponse(int UpdatedUsers, int NonMemberCount, UpdateRoleResponse[] UpdateResponses);

		public async Task<UpdateRolesResponse> UpdateRolesAndDb()
		{
			List<DdUser> dbUsersList = _databaseHelper.GetUsers();

			IEnumerable<DdUser> usersInGuild = dbUsersList.Where(dbUser => _guild.GetUser(dbUser.DiscordId) != null);

			int nonMemberCount = dbUsersList.Count - usersInGuild.Count();

			List<Task<UpdateRoleResponse>> tasks = new();
			foreach (DdUser user in usersInGuild)
				tasks.Add(UpdateUserRoles(user));

			UpdateRoleResponse[] responses = await Task.WhenAll(tasks);
			int usersUpdated = responses.Count(b => b.Success);
			return new(usersUpdated, nonMemberCount, responses);
		}

		public record UpdateRoleResponse(bool Success, List<SocketRole>? AddedRoles, List<SocketRole>? RemovedRoles, SocketGuildUser? GuildMember);

		public async Task<UpdateRoleResponse> UpdateUserRoles(DdUser user)
		{
			SocketGuildUser guildMember = _guild.GetUser(user.DiscordId);

			DdPlayer ddPlayer = await GetDdPlayer(user.LeaderboardId);
			_databaseHelper.FindAndUpdateUser(guildMember.Id, ddPlayer.Time / 10000);

			KeyValuePair<int, ulong> scoreRole = _scoreRoles.ScoreRoleDictionary.Where(sr => sr.Key <= ddPlayer.Time / 10000).OrderByDescending(sr => sr.Key).FirstOrDefault();
			SocketRole scoreRoleToAdd = _guild.GetRole(scoreRole.Value);
			List<SocketRole> addedRoles = new(), removedRoles = new();

			HandleTopRolesResponse response = HandleTopRoles(guildMember, ddPlayer.Rank);

			if (!MemberHasRole(guildMember, scoreRoleToAdd.Id))
				addedRoles.Add(scoreRoleToAdd);

			addedRoles.AddRange(response.TopRolesToadd);
			removedRoles.AddRange(response.TopRolesToRemove);
			removedRoles.AddRange(GetScoreRolesToRemove(guildMember, scoreRoleToAdd));

			await guildMember.AddRolesAsync(addedRoles);
			await guildMember.RemoveRolesAsync(removedRoles);

			if (removedRoles.Count == 0 && addedRoles.Count == 0)
				return new(false, null, null, null);

			return new(true, addedRoles, removedRoles, guildMember);
		}

		private static async Task<DdPlayer> GetDdPlayer(int lbId)
		{
			using HttpClient client = new();
			string jsonUser = await client.GetStringAsync($"https://devildaggers.info/api/leaderboards/user/by-id?userId={lbId}");

			return JsonConvert.DeserializeObject<DdPlayer>(jsonUser);
		}

		private List<SocketRole> GetScoreRolesToRemove(SocketGuildUser member, SocketRole excludedRole)
		{
			List<SocketRole> removedRoles = new List<SocketRole>();

			const ulong newMemberRoleId = 728663492424499200;
			IEnumerable<SocketRole> rolesToRemove = member.Roles.Where(r =>
				_scoreRoles.ScoreRoleDictionary.ContainsValue(r.Id) &&
				r.Id != excludedRole.Id ||
				r.Id == newMemberRoleId);

			removedRoles.AddRange(rolesToRemove);

			return removedRoles;
		}

		private static List<SocketRole> GetTopRolesToRemove(SocketGuildUser member, SocketRole excludedRole)
		{
			List<ulong> rolesToRemove = new() { _wrRoleId, _top3RoleId, _top10RoleId };
			rolesToRemove = rolesToRemove.Where(r => r != excludedRole.Id).ToList();
			List<SocketRole> removedRoles = new();

			removedRoles.AddRange(member.Roles.Where(r => rolesToRemove.Contains(r.Id)));

			return removedRoles;
		}

		public record HandleTopRolesResponse(List<SocketRole> TopRolesToadd, List<SocketRole> TopRolesToRemove);

		private HandleTopRolesResponse HandleTopRoles(SocketGuildUser member, int rank)
		{
			SocketRole wrRole = _guild.GetRole(_wrRoleId);
			SocketRole top3Role = _guild.GetRole(_top3RoleId);
			SocketRole top10Role = _guild.GetRole(_top10RoleId);

			List<SocketRole> topRolesToAdd = new(), topRolesToRemove = new();
			SocketRole? roleToExclude = null;
			if (rank < 11)
			{
				if (rank == 1)
				{
					if (!member.Roles.Contains(wrRole))
						topRolesToAdd.Add(wrRole);

					roleToExclude = wrRole;
				}
				else if (rank < 4)
				{
					if (!member.Roles.Contains(top3Role))
						topRolesToAdd.Add(top3Role);

					roleToExclude = top3Role;
				}
				else
				{
					if (!member.Roles.Contains(top10Role))
						topRolesToAdd.Add(top10Role);

					roleToExclude = top10Role;
				}
			}

			if (roleToExclude != null)
				topRolesToRemove = GetTopRolesToRemove(member, roleToExclude);

			return new(topRolesToAdd, topRolesToRemove);
		}

		private static bool MemberHasRole(SocketGuildUser member, ulong roleId)
			=> member.Roles.Any(role => role.Id == roleId);
	}
}
