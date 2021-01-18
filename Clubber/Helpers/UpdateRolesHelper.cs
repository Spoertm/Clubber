using Clubber.Database;
using Clubber.Files;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public static class UpdateRolesHelper
	{
		public static async Task<DatabaseUpdateResponse> UpdateRolesAndDb(SocketGuild guild)
		{
			List<DdUser> usersList = DatabaseHelper.DdUsers;
			IEnumerable<SocketGuildUser> registeredUsersInGuild = guild.Users.Where(u => usersList.Any(du => du.DiscordId == u.Id));

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
			try
			{
				List<DdUser> usersList = DatabaseHelper.DdUsers;
				DdUser ddUser = usersList.Find(du => du.DiscordId == user.Id)!;
				dynamic lbPlayer = await DatabaseHelper.GetLbPlayer((uint)ddUser.LeaderboardId);

				IEnumerable<ulong> userRolesIds = user.Roles.Select(r => r.Id);
				(IEnumerable<ulong> scoreRoleToAdd, IEnumerable<ulong> scoreRolesToRemove) = HandleScoreRoles(userRolesIds, (int)lbPlayer.time);
				(IEnumerable<ulong> topRoleToAdd, IEnumerable<ulong> topRolesToRemove) = HandleTopRoles(userRolesIds, (int)lbPlayer.rank);

				if (!scoreRoleToAdd.Any() && !scoreRolesToRemove.Any() && !topRoleToAdd.Any() && !topRolesToRemove.Any())
					return new(false, null, null, null);

				IEnumerable<SocketRole> socketRolesToAdd = scoreRoleToAdd.Concat(topRoleToAdd).Select(r => user.Guild.GetRole(r));
				IEnumerable<SocketRole> socketRolesToRemove = scoreRolesToRemove.Concat(topRolesToRemove).Select(r => user.Guild.GetRole(r));

				await user.AddRolesAsync(socketRolesToAdd);
				await user.RemoveRolesAsync(socketRolesToRemove);

				return new(true, user, socketRolesToAdd, socketRolesToRemove);
			}
			catch (CustomException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new CustomException("Something went wrong. Chupacabra will get on it soon:tm:.", ex);
			}
		}

		private static (IEnumerable<ulong> ScoreRoleToAdd, IEnumerable<ulong> ScoreRolesToRemove) HandleScoreRoles(IEnumerable<ulong> userRolesIds, int playerTime)
		{
			KeyValuePair<int, ulong> scoreRole = Constants.ScoreRoles.FirstOrDefault(sr => sr.Key <= playerTime / 10000);

			List<ulong> scoreRoleToAdd = new();
			if (!userRolesIds.Contains(scoreRole.Value))
				scoreRoleToAdd.Add(scoreRole.Value);

			IEnumerable<ulong> filteredScoreRoles = Constants.ScoreRoles.Values.Where(rid => rid != scoreRole.Value).Concat(Constants.UselessRoles);
			return (scoreRoleToAdd, userRolesIds.Intersect(filteredScoreRoles));
		}

		private static (IEnumerable<ulong> TopRoleToAdd, IEnumerable<ulong> TopRolesToRemove) HandleTopRoles(IEnumerable<ulong> userRolesIds, int rank)
		{
			KeyValuePair<int, ulong>? rankRole = Constants.RankRoles.FirstOrDefault(rr => rank <= rr.Key);

			List<ulong> topRoleToAdd = new();
			if (rankRole.Value.Value == 0)
				return new(topRoleToAdd, userRolesIds.Intersect(Constants.RankRoles.Values));

			if (!userRolesIds.Contains(rankRole.Value.Value))
				topRoleToAdd.Add(rankRole.Value.Value);

			IEnumerable<ulong> filteredTopRoles = Constants.RankRoles.Values.Where(rid => rid != rankRole.Value.Value);
			return new(topRoleToAdd, userRolesIds.Intersect(filteredTopRoles));
		}
	}

	public record UpdateRolesResponse(bool Success, SocketGuildUser? User, IEnumerable<SocketRole>? RolesAdded, IEnumerable<SocketRole>? RolesRemoved);
	public record DatabaseUpdateResponse(int NonMemberCount, int UpdatedUsers, UpdateRolesResponse[] UpdateResponses);
}
