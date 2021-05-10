using Clubber.Models;
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
		private static readonly Dictionary<int, ulong> _scoreRoles = new()
		{
			[1200] = 626477161825697803,
			[1180] = 728017461911355454,
			[1160] = 626476794878623756,
			[1140] = 626476562128044052,
			[1120] = 525082045614129163,
			[1100] = 402530230109208577,
			[1075] = 525733825934786570,
			[1050] = 399577125180669963,
			[1025] = 525967813551325196,
			[1000] = 399570979610820608,
			[950] = 728017240762482788,
			[900] = 399570895741386765,
			[800] = 399570790506299398,
			[700] = 399570712018288640,
			[600] = 399569864261632001,
			[500] = 399569581561217024,
			[400] = 399569447771439104,
			[300] = 399569332532674562,
			[200] = 399569259182948363,
			[100] = 399569183966363648,
			[0] = 461203024128376832,
		};
		private static readonly List<ulong> _uselessRoles = new()
		{
			728663492424499200, 458375331468935178,
		};
		private static readonly Dictionary<int, ulong> _rankRoles = new()
		{
			[1] = 446688666325090310, [3] = 472451008342261820, [10] = 556255819323277312,
		};
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
			List<(DdUser DdUser, SocketGuildUser GuildUser)> registeredUsers = dbUsers.Join(
					inner: guildUsers,
					outerKeySelector: dbu => dbu.DiscordId,
					innerKeySelector: gu => gu.Id,
					resultSelector: (ddUser, guildUser) => (ddUser, guildUser))
				.ToList();

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

			return (dbUsers.Count - registeredUsers.Count, responses);
		}

		public async Task<UpdateRolesResponse> UpdateUserRoles(SocketGuildUser user)
		{
			try
			{
				int lbId = _databaseHelper.GetDdUserByDiscordId(user.Id)!.LeaderboardId;
				List<LeaderboardUser> lbPlayerList = await _webService.GetLbPlayers(new[]
				{
					(uint)lbId,
				});

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
			ulong[] userRolesIds = guildUser.Roles.Select(r => r.Id).ToArray();
			(ulong scoreRoleToAdd, ulong[] scoreRolesToRemove) = HandleScoreRoles(userRolesIds, lbUser.Time);
			(ulong topRoleToAdd, ulong[] topRolesToRemove) = HandleTopRoles(userRolesIds, lbUser.Rank);

			if (scoreRoleToAdd == 0 && scoreRolesToRemove.Length == 0 && topRoleToAdd == 0 && topRolesToRemove.Length == 0)
				return new(false, null, null, null);

			SocketRole[] socketRolesToAdd =
			{
				guildUser.Guild.GetRole(scoreRoleToAdd), guildUser.Guild.GetRole(topRoleToAdd),
			};

			SocketRole[] socketRolesToRemove = scoreRolesToRemove.Concat(topRolesToRemove)
				.Select(r => guildUser.Guild.GetRole(r))
				.ToArray();

			await guildUser.AddRolesAsync(socketRolesToAdd);
			await guildUser.RemoveRolesAsync(socketRolesToRemove);

			return new(true, guildUser, socketRolesToAdd, socketRolesToRemove);
		}

		private static (ulong ScoreRoleToAdd, ulong[] ScoreRolesToRemove) HandleScoreRoles(ulong[] userRolesIds, int playerTime)
		{
			(_, ulong scoreRoleId) = _scoreRoles.FirstOrDefault(sr => sr.Key <= playerTime / 10000);

			ulong scoreRoleToAdd = 0;
			if (!userRolesIds.Contains(scoreRoleId))
				scoreRoleToAdd = scoreRoleId;

			IEnumerable<ulong> filteredScoreRoles = _scoreRoles.Values.Where(rid => rid != scoreRoleId).Concat(_uselessRoles);
			return (scoreRoleToAdd, userRolesIds.Intersect(filteredScoreRoles).ToArray());
		}

		private static (ulong TopRoleToAdd, ulong[] TopRolesToRemove) HandleTopRoles(ulong[] userRolesIds, int rank)
		{
			KeyValuePair<int, ulong>? rankRole = _rankRoles.FirstOrDefault(rr => rank <= rr.Key);

			ulong topRoleToAdd = 0;
			if (rankRole.Value.Value == 0)
				return new(topRoleToAdd, userRolesIds.Intersect(_rankRoles.Values).ToArray());

			if (!userRolesIds.Contains(rankRole.Value.Value))
				topRoleToAdd = rankRole.Value.Value;

			IEnumerable<ulong> filteredTopRoles = _rankRoles.Values.Where(rid => rid != rankRole.Value.Value);
			return new(topRoleToAdd, userRolesIds.Intersect(filteredTopRoles).ToArray());
		}
	}

	public record UpdateRolesResponse(bool Success, SocketGuildUser? User, IEnumerable<SocketRole>? RolesAdded, IEnumerable<SocketRole>? RolesRemoved);
	public record DatabaseUpdateResponse(string Message, Embed[] RoleUpdateEmbeds);
}
