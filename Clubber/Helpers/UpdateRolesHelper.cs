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
		public static async Task<DatabaseUpdateResponse> UpdateRolesAndDb(IEnumerable<SocketGuildUser> guildUsers)
		{
			List<DdUser> dbUsers = DatabaseHelper.DdUsers;

			IEnumerable<(DdUser DdUser, SocketGuildUser GuildUser)> registeredUsers = dbUsers.Join(
				inner: guildUsers,
				outerKeySelector: dbu => dbu.DiscordId,
				innerKeySelector: gu => gu.Id,
				resultSelector: (ddUser, guildUser) => (ddUser, guildUser));

			IEnumerable<uint> lbIdsToRequest = registeredUsers.Select(ru => (uint)ru.DdUser.LeaderboardId);
			IEnumerable<LeaderboardUser> lbPlayers = await DatabaseHelper.GetLbPlayers(lbIdsToRequest);

			List<UpdateRolesResponse> responses = new();
			Parallel.ForEach(registeredUsers, async user => responses.Add(await ExecuteRoleUpdate(user.GuildUser, lbPlayers.First(lbp => lbp.Id == user.DdUser.LeaderboardId))));

			return new(dbUsers.Count - registeredUsers.Count(), responses);
		}

		public static async Task<UpdateRolesResponse> UpdateUserRoles(SocketGuildUser user)
		{
			try
			{
				int lbId = DatabaseHelper.DdUsers.Find(ddu => ddu.DiscordId == user.Id)!.LeaderboardId;
				IEnumerable<LeaderboardUser> lbPlayerList = await DatabaseHelper.GetLbPlayers(new uint[] { (uint)lbId });
				return await ExecuteRoleUpdate(user, lbPlayerList.First());
			}
			catch (CustomException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new CustomException("Something went wrong. Chupacabra will get on it soon™.", ex);
			}
		}

		private static async Task<UpdateRolesResponse> ExecuteRoleUpdate(SocketGuildUser guildUser, LeaderboardUser lbUser)
		{
			IEnumerable<ulong> userRolesIds = guildUser.Roles.Select(r => r.Id);
			(IEnumerable<ulong> scoreRoleToAdd, IEnumerable<ulong> scoreRolesToRemove) = HandleScoreRoles(userRolesIds, lbUser.Time);
			(IEnumerable<ulong> topRoleToAdd, IEnumerable<ulong> topRolesToRemove) = HandleTopRoles(userRolesIds, lbUser.Rank);

			if (!scoreRoleToAdd.Any() && !scoreRolesToRemove.Any() && !topRoleToAdd.Any() && !topRolesToRemove.Any())
				return new(false, null, null, null);

			IEnumerable<SocketRole> socketRolesToAdd = scoreRoleToAdd.Concat(topRoleToAdd).Select(r => guildUser.Guild.GetRole(r));
			IEnumerable<SocketRole> socketRolesToRemove = scoreRolesToRemove.Concat(topRolesToRemove).Select(r => guildUser.Guild.GetRole(r));

			await guildUser.AddRolesAsync(socketRolesToAdd);
			await guildUser.RemoveRolesAsync(socketRolesToRemove);

			return new(true, guildUser, socketRolesToAdd, socketRolesToRemove);
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
	public record DatabaseUpdateResponse(int NonMemberCount, IEnumerable<UpdateRolesResponse> UpdateResponses);
}
