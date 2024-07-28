using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Microsoft.Extensions.Options;
using Serilog;
using System.Diagnostics;

namespace Clubber.Discord.Services;

public class ScoreRoleService
{
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IWebService _webService;
	private readonly IReadOnlyList<ulong> _uselessRoles;

	public ScoreRoleService(IOptions<AppConfig> config, IDatabaseHelper databaseHelper, IWebService webService)
	{
		_databaseHelper = databaseHelper;
		_webService = webService;

		_uselessRoles = [config.Value.UnregisteredRoleId, 458375331468935178, 994354086646399066];
	}

	public async Task<DatabaseUpdateResponse> UpdateRolesAndDb(IReadOnlyCollection<IGuildUser> guildUsers)
	{
		Stopwatch sw = Stopwatch.StartNew();
		(int nonMemberCount, List<UpdateRolesResponse> updateRolesResponses) = await ExecuteRolesAndDbUpdate(guildUsers);
		sw.Stop();

		int updatedUsers = 0;
		List<Embed> embedList = [];
		foreach (UpdateRolesResponse.Full updateResponse in updateRolesResponses.OfType<UpdateRolesResponse.Full>())
		{
			embedList.Add(EmbedHelper.UpdateRoles(updateResponse));
			updatedUsers++;
		}

		string message = updatedUsers > 0
			? $"✅ Successfully updated database and {updatedUsers} user(s).\n🕐 Execution took {sw.ElapsedMilliseconds} ms."
			: $"No updates needed today.\nExecution took {sw.ElapsedMilliseconds} ms.";

		if (nonMemberCount > 0)
			message += $"\nℹ️ {nonMemberCount} user(s) are registered but aren't in the server.";

		return new(message, embedList.ToArray());
	}

	private async Task<(int NonMemberCount, List<UpdateRolesResponse> UpdateRolesResponses)> ExecuteRolesAndDbUpdate(IReadOnlyCollection<IGuildUser> guildUsers)
	{
		List<DdUser> dbUsers = await _databaseHelper.GetEntireDatabase();
		List<(DdUser DdUser, IGuildUser GuildUser)> registeredUsers = dbUsers.Join(
				inner: guildUsers,
				outerKeySelector: dbu => dbu.DiscordId,
				innerKeySelector: gu => gu.Id,
				resultSelector: (ddUser, guildUser) => (ddUser, guildUser))
			.ToList();

		IEnumerable<uint> lbIdsToRequest = registeredUsers.Select(ru => ru.DdUser.LeaderboardId);
		IReadOnlyList<EntryResponse> lbPlayers = await _webService.GetLbPlayers(lbIdsToRequest);

		IEnumerable<(IGuildUser GuildUser, EntryResponse LbUser)> updatedUsers = registeredUsers.Join(
				inner: lbPlayers,
				outerKeySelector: ru => ru.DdUser.LeaderboardId,
				innerKeySelector: lbp => (uint)lbp.Id,
				resultSelector: (ru, lbp) => (ru.GuildUser, lbp))
			.ToArray();

		List<UpdateRolesResponse> responses = [];

		foreach ((IGuildUser guildUser, EntryResponse lbUser) in updatedUsers)
			responses.Add(await ExecuteRoleUpdate(guildUser, lbUser));

		return (dbUsers.Count - registeredUsers.Count, responses);
	}

	public async Task<UpdateRolesResponse> UpdateUserRoles(IGuildUser user)
	{
		try
		{
			DdUser ddUser = await _databaseHelper.FindRegisteredUser(user.Id) ?? throw new ClubberException("User not found in database.");
			uint lbId = ddUser.LeaderboardId;
			IReadOnlyList<EntryResponse> lbPlayerList = await _webService.GetLbPlayers([lbId]);

			return await ExecuteRoleUpdate(user, lbPlayerList[0]);
		}
		catch (ClubberException clubberException)
		{
			Log.Error(clubberException, "Error updating user roles");
			throw;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error updating user roles");
			throw new ClubberException("Something went wrong. Chupacabra will get on it soon™.", ex);
		}
	}

	private async Task<UpdateRolesResponse> ExecuteRoleUpdate(IGuildUser guildUser, EntryResponse lbUser)
	{
		(ulong scoreRoleToAdd, ulong[] scoreRolesToRemove) = HandleScoreRoles(guildUser.RoleIds, lbUser.Time);
		(ulong topRoleToAdd, ulong[] topRolesToRemove) = HandleTopRoles(guildUser.RoleIds, lbUser.Rank);
		(decimal secondsAwayFromNextRole, ulong nextRoleId) = GetSecondsAwayFromNextRoleAndNextRoleId(lbUser.Time);

		if (scoreRoleToAdd == 0 && scoreRolesToRemove.Length == 0 && topRoleToAdd == 0 && topRolesToRemove.Length == 0)
			return new UpdateRolesResponse.Partial(secondsAwayFromNextRole, nextRoleId);

		List<ulong> roleIdsToAdd = new(2);
		if (scoreRoleToAdd != 0)
			roleIdsToAdd.Add(scoreRoleToAdd);

		if (topRoleToAdd != 0)
			roleIdsToAdd.Add(topRoleToAdd);

		ulong[] socketRolesToRemove = scoreRolesToRemove.Concat(topRolesToRemove)
			.ToArray();

		if (roleIdsToAdd.Count > 0)
			await guildUser.AddRolesAsync(roleIdsToAdd);

		if (socketRolesToRemove.Length > 0)
			await guildUser.RemoveRolesAsync(socketRolesToRemove);

		return new UpdateRolesResponse.Full(
			guildUser,
			roleIdsToAdd,
			socketRolesToRemove);
	}

	public (ulong ScoreRoleToAdd, ulong[] ScoreRolesToRemove) HandleScoreRoles(IReadOnlyCollection<ulong> userRolesIds, int playerTime)
	{
		(_, ulong scoreRoleId) = AppConfig.ScoreRoles.FirstOrDefault(sr => sr.Key <= playerTime / 10_000);

		ulong scoreRoleToAdd = 0;
		if (!userRolesIds.Contains(scoreRoleId))
			scoreRoleToAdd = scoreRoleId;

		IEnumerable<ulong> filteredScoreRoles = AppConfig.ScoreRoles.Values.Where(rid => rid != scoreRoleId).Concat(_uselessRoles);
		return (scoreRoleToAdd, userRolesIds.Intersect(filteredScoreRoles).ToArray());
	}

	public (ulong TopRoleToAdd, ulong[] TopRolesToRemove) HandleTopRoles(IReadOnlyCollection<ulong> userRolesIds, int rank)
	{
		KeyValuePair<int, ulong>? rankRole = AppConfig.RankRoles.FirstOrDefault(rr => rank <= rr.Key);

		ulong topRoleToAdd = 0;
		if (rankRole.Value.Value == 0)
			return new(topRoleToAdd, userRolesIds.Intersect(AppConfig.RankRoles.Values).ToArray());

		if (!userRolesIds.Contains(rankRole.Value.Value))
			topRoleToAdd = rankRole.Value.Value;

		IEnumerable<ulong> filteredTopRoles = AppConfig.RankRoles.Values.Where(rid => rid != rankRole.Value.Value);
		return new(topRoleToAdd, userRolesIds.Intersect(filteredTopRoles).ToArray());
	}

	public (decimal SecondsAwayFromNextRole, ulong NextRoleId) GetSecondsAwayFromNextRoleAndNextRoleId(int playerTime)
	{
		(int score, _) = AppConfig.ScoreRoles.FirstOrDefault(sr => sr.Key <= playerTime / 10_000);

		if (score == AppConfig.ScoreRoles.Keys.Max())
		{
			return default;
		}

		(int nextScore, ulong nextRoleId) = AppConfig.ScoreRoles.Last(sr => sr.Key > playerTime / 10_000);
		decimal secondsAwayFromNextRole = nextScore - playerTime / 10_000M;

		return (secondsAwayFromNextRole, nextRoleId);
	}
}
