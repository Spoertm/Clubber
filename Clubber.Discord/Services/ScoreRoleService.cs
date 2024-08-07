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
using System.Runtime.Serialization;

namespace Clubber.Discord.Services;

public class ScoreRoleService
{
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IWebService _webService;
	private readonly IReadOnlyCollection<ulong> _allPossibleRoles;

	public ScoreRoleService(IOptions<AppConfig> config, IDatabaseHelper databaseHelper, IWebService webService)
	{
		_databaseHelper = databaseHelper;
		_webService = webService;

		IReadOnlyCollection<ulong> uselessRoles = [config.Value.UnregisteredRoleId, 458375331468935178, 994354086646399066];
		_allPossibleRoles = [..AppConfig.ScoreRoles.Values, ..AppConfig.RankRoles.Values, ..uselessRoles];
	}

	public async Task<BulkUserRoleUpdates> GetBulkUserRoleUpdates(IReadOnlyCollection<IGuildUser> guildUsers)
	{
		List<DdUser> dbUsers = await _databaseHelper.GetRegisteredUsers();
		(DdUser ddUser, IGuildUser guildUser)[] registeredUsers = dbUsers.Join(
				inner: guildUsers,
				outerKeySelector: dbu => dbu.DiscordId,
				innerKeySelector: gu => gu.Id,
				resultSelector: (ddUser, guildUser) => (ddUser, guildUser))
			.ToArray();

		IEnumerable<uint> lbIdsToRequest = registeredUsers.Select(ru => (uint)ru.ddUser.LeaderboardId);
		IReadOnlyList<EntryResponse> lbPlayers = await _webService.GetLbPlayers(lbIdsToRequest);

		(IGuildUser guildUser, EntryResponse lbPlayer)[] registeredDiscordLbPlayers = registeredUsers.Join(
				inner: lbPlayers,
				outerKeySelector: ru => (uint)ru.ddUser.LeaderboardId,
				innerKeySelector: lbp => (uint)lbp.Id,
				resultSelector: (ru, lbp) => (ru.guildUser, lbp))
			.ToArray();

		List<UserRoleUpdate> roleUpdates = [];
		foreach ((IGuildUser? guildUser, EntryResponse? lbPlayer) in registeredDiscordLbPlayers)
		{
			RoleChangeResult roleChangeResult = GetRoleChange(guildUser.RoleIds, lbPlayer);
			if (roleChangeResult is RoleUpdate roleUpdate)
			{
				roleUpdates.Add(new(guildUser, roleUpdate));
			}
		}

		return new(dbUsers.Count - registeredUsers.Length, roleUpdates);
	}

	public async Task<Result<RoleChangeResult>> GetRoleChange(IGuildUser user)
	{
		try
		{
			DdUser? ddUser = await _databaseHelper.FindRegisteredUser(user.Id);
			if (ddUser is null)
			{
				return Result.Failure<RoleChangeResult>("User is not registered.")!;
			}

			uint lbId = (uint)ddUser.LeaderboardId;
			IReadOnlyList<EntryResponse> lbPlayerList = await _webService.GetLbPlayers([lbId]);

			return Result.Success(GetRoleChange(user.RoleIds, lbPlayerList[0]));
		}
		catch (Exception ex)
		{
			string errorMsg = ex switch
			{
				ClubberException       => ex.Message,
				HttpRequestException   => "Couldn't fetch player data. Ddinfo may be down.",
				SerializationException => "Couldn't read player data.",
				_                      => "Internal error.",
			};

			Log.Error(ex, "Error updating user roles");
			return Result.Failure<RoleChangeResult>(errorMsg)!;
		}
	}

	private RoleChangeResult GetRoleChange(IReadOnlyCollection<ulong> roleIds, EntryResponse lbUser)
	{
		ulong scoreRoleToKeep = GetScoreRoleToKeep(lbUser.Time).Value;
		ulong rankRoleToKeep = GetRankRoleToKeep(lbUser.Rank).Value;

		List<ulong> rolesToKeep = [scoreRoleToKeep];
		if (rankRoleToKeep != default)
		{
			rolesToKeep.Add(rankRoleToKeep);
		}

		CollectionChange<ulong> collectionChange = CollectionUtils.DetermineCollectionChanges(roleIds, _allPossibleRoles, rolesToKeep);

		if (collectionChange.ItemsToAdd.Length == 0 && collectionChange.ItemsToRemove.Length == 0)
		{
			MilestoneInfo<ulong> milestoneInfo = CollectionUtils.GetNextMileStone(lbUser.Time, AppConfig.ScoreRoles);
			return RoleChangeResult.None.FromMileStoneInfo(milestoneInfo);
		}

		return RoleUpdate.FromCollectionChanges(collectionChange);
	}

	public static KeyValuePair<int, ulong> GetScoreRoleToKeep(int playerTime)
	{
		return AppConfig.ScoreRoles.First(sr => sr.Key <= playerTime / 10_000);
	}

	public static KeyValuePair<int, ulong> GetRankRoleToKeep(int playerRank)
	{
		return AppConfig.RankRoles.FirstOrDefault(rr => playerRank <= rr.Key);
	}
}
