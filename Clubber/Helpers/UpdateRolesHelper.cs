using Clubber.Database;
using Clubber.Files;
using Clubber.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class UpdateRolesHelper
	{
		private readonly DatabaseHelper _databaseHelper;
		private readonly WebService _webService;

		public UpdateRolesHelper(DatabaseHelper databaseHelper, WebService webService)
		{
			_databaseHelper = databaseHelper;
			_webService = webService;
		}

		public async Task<DatabaseUpdateResponse> UpdateRolesAndDb(IEnumerable<SocketGuildUser> guildUsers)
		{
			Stopwatch sw = Stopwatch.StartNew();
			(int nonMemberCount, List<UpdateRolesResponse> updateRolesResponses) = await ExecuteRolesAndDbUpdate(guildUsers);
			sw.Stop();

			string message = string.Empty;

			int updatedUsers = 0;
			List<Embed> embedList = new();
			foreach (UpdateRolesResponse updateResponse in updateRolesResponses.Where(ur => ur.Success))
			{
				embedList.Add(EmbedHelper.UpdateRoles(updateResponse));
				updatedUsers++;
			}

			if (updatedUsers > 0)
				message += $"✅ Successfully updated database and {updatedUsers} user(s).\n🕐 Execution took {sw.ElapsedMilliseconds} ms.\n";
			else
				message += $"No updates needed today.\nExecution took {sw.ElapsedMilliseconds} ms.\n";

			if (nonMemberCount > 0)
				message += $"ℹ️ {nonMemberCount} user(s) are registered but aren't in the server.";

			return new(message, embedList.ToArray());
		}

		private async Task<(int NonMemberCount, List<UpdateRolesResponse> UpdateRolesResponses)> ExecuteRolesAndDbUpdate(IEnumerable<SocketGuildUser> guildUsers)
		{
			List<DdUser> dbUsers = _databaseHelper.Database;

			IEnumerable<(DdUser DdUser, SocketGuildUser GuildUser)> registeredUsers = dbUsers.Join(
				inner: guildUsers,
				outerKeySelector: dbu => dbu.DiscordId,
				innerKeySelector: gu => gu.Id,
				resultSelector: (ddUser, guildUser) => (ddUser, guildUser));

			IEnumerable<uint> lbIdsToRequest = registeredUsers.Select(ru => (uint)ru.DdUser.LeaderboardId);
			IEnumerable<LeaderboardUser> lbPlayers = await _webService.GetLbPlayers(lbIdsToRequest);

			(SocketGuildUser GuildUser, LeaderboardUser LbUser)[] updatedUsers = registeredUsers.Join(
				inner: lbPlayers,
				outerKeySelector: ru => ru.DdUser.LeaderboardId,
				innerKeySelector: lbp => lbp.Id,
				resultSelector: (ru, lbp) => (ru.GuildUser, lbp))
				.ToArray();

			List<UpdateRolesResponse> responses = new();

			for (int i = 0; i < updatedUsers.Length; i++)
				responses.Add(await ExecuteRoleUpdate(updatedUsers[i].GuildUser, updatedUsers[i].LbUser));

			return (dbUsers.Count - registeredUsers.Count(), responses);
		}

		public async Task<UpdateRolesResponse> UpdateUserRoles(SocketGuildUser user)
		{
			try
			{
				int lbId = _databaseHelper.GetDdUserByDiscordId(user.Id)!.LeaderboardId;
				List<LeaderboardUser> lbPlayerList = await _webService.GetLbPlayers(new uint[] { (uint)lbId });
				return await ExecuteRoleUpdate(user, lbPlayerList[0]);
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
	public record DatabaseUpdateResponse(string Message, Embed[] RoleUpdateEmbeds);
}
