using Clubber.Files;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public static class UpdateRolesHelper
	{
		private static Dictionary<int, ulong> ScoreRoleDictionary => JsonConvert.DeserializeObject<Dictionary<int, ulong>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Database", "ScoreRoles.json")));

		public static async Task<DatabaseUpdateResponse> UpdateRolesAndDb(SocketGuild guild)
		{
			List<DdUser> usersList = DatabaseHelper.DdUsers;
			IEnumerable<SocketGuildUser> registeredUsersInGuild = guild.Users.Where(u => usersList.Any(du => du.DiscordId == u.Id)); // "guild users that are in the list"
			// Alternatively: usersList.Where(du => guild.GetUser(du.DiscordId) != null); which would mean "dd users that are in the guild"

			int nonMemberCount = usersList.Count - registeredUsersInGuild.Count();

			List<Task<UpdateRolesResponse>> tasks = new();
			foreach (SocketGuildUser user in registeredUsersInGuild)
				tasks.Add(UpdateUserRoles(user));

			UpdateRolesResponse[] responses = await Task.WhenAll(tasks);
			int usersUpdated = responses.Count(b => b.Success);
			return new(nonMemberCount, usersUpdated, responses);
		}

		public static async Task<UpdateRolesResponse> UpdateUserRoles(SocketGuildUser user)
		{
			List<DdUser> usersList = DatabaseHelper.DdUsers;
			DdUser ddUser = usersList.Find(du => du.DiscordId == user.Id)!;
			dynamic lbPlayer = await DatabaseHelper.GetLbPlayer((uint)ddUser.LeaderboardId);

			List<ulong> rolesToAdd = new(), rolesToRemove = new();
			ulong scoreRoleToAdd = ScoreRoleDictionary.FirstOrDefault(sr => sr.Key <= (int)lbPlayer!.time / 10000).Value;
			IReadOnlyCollection<SocketRole> userRoles = user.Roles;

			(IEnumerable<ulong> topRolesToAdd, IEnumerable<ulong> topRolesToRemove) = HandleTopRoles(userRoles, (int)lbPlayer!.rank);

			if (!userRoles.Any(r => r.Id == scoreRoleToAdd))
				rolesToAdd.Add(scoreRoleToAdd);

			rolesToAdd.AddRange(topRolesToAdd);
			rolesToRemove.AddRange(topRolesToRemove);
			rolesToRemove.AddRange(GetScoreRolesToRemove(userRoles, scoreRoleToAdd));

			if (rolesToRemove.Count == 0 && rolesToAdd.Count == 0)
				return new(false, null, null, null);

			IEnumerable<SocketRole> socketRolesToAdd = rolesToAdd.Select(r => user.Guild.GetRole(r));
			IEnumerable<SocketRole> socketRolesToRemove = rolesToRemove.Select(r => user.Guild.GetRole(r));

			await user.AddRolesAsync(socketRolesToAdd);
			await user.RemoveRolesAsync(socketRolesToRemove);

			return new(true, user, socketRolesToAdd, socketRolesToRemove);
		}

		private static IEnumerable<ulong> GetScoreRolesToRemove(IEnumerable<SocketRole> userRoles, ulong excludedRole)
		{
			return userRoles.Where(r =>
				ScoreRoleDictionary.ContainsValue(r.Id) &&
				r.Id != excludedRole ||
				r.Id == Constants.MembersRoleId)
				.Select(r => r.Id);
		}

		private static IEnumerable<ulong> GetTopRolesToRemove(IEnumerable<SocketRole> userRoles, ulong excludedRole)
		{
			List<ulong> rolesToRemove = new() { Constants.WrRoleId, Constants.Top3RoleId, Constants.Top10RoleId };
			rolesToRemove = rolesToRemove.Where(r => r != excludedRole).ToList();

			return userRoles.Where(r => rolesToRemove.Contains(r.Id)).Select(r => r.Id);
		}

		private static (IEnumerable<ulong> TopRolesToAdd, IEnumerable<ulong> TopRolesToRemove) HandleTopRoles(IEnumerable<SocketRole> userRoles, int rank)
		{
			List<ulong> topRolesToAdd = new(), topRolesToRemove = new();
			ulong roleToExclude = 0;

			if (rank == 1)
			{
				if (!userRoles.Any(r => r.Id == Constants.WrRoleId))
					topRolesToAdd.Add(Constants.WrRoleId);

				roleToExclude = Constants.WrRoleId;
			}
			else if (rank < 4)
			{
				if (!userRoles.Any(r => r.Id == Constants.Top3RoleId))
					topRolesToAdd.Add(Constants.Top3RoleId);

				roleToExclude = Constants.Top3RoleId;
			}
			else
			{
				if (!userRoles.Any(r => r.Id == Constants.Top10RoleId))
					topRolesToAdd.Add(Constants.Top10RoleId);

				roleToExclude = Constants.Top10RoleId;
			}

			if (roleToExclude != 0)
				topRolesToRemove = GetTopRolesToRemove(userRoles, roleToExclude).ToList();

			return new(topRolesToAdd, topRolesToRemove);
		}
	}

	public record UpdateRolesResponse(bool Success, SocketGuildUser? User, IEnumerable<SocketRole>? RolesAdded, IEnumerable<SocketRole>? RolesRemoved);
	public record DatabaseUpdateResponse(int NonMemberCount, int UpdatedUsers, UpdateRolesResponse[] UpdateResponses);
}
