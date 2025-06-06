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

public sealed class ScoreRoleService
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
		IEnumerable<ulong> guildUserIds = guildUsers.Select(gu => gu.Id);

		Task<int> allUsersTask = _databaseHelper.GetRegisteredUserCount();
		Task<List<DdUser>> filteredUsersTask = _databaseHelper.GetRegisteredUsers(guildUserIds);

		await Task.WhenAll(allUsersTask, filteredUsersTask);

		int registeredUserCount = await allUsersTask;
		List<DdUser> dbUsers = await filteredUsersTask;

		Dictionary<ulong, IGuildUser> guildUserLookup = guildUsers.ToDictionary(gu => gu.Id);

		List<(DdUser ddUser, IGuildUser guildUser)> registeredDiscordUsers = new(dbUsers.Count);
		foreach (DdUser dbUser in dbUsers)
		{
			if (guildUserLookup.TryGetValue(dbUser.DiscordId, out IGuildUser? guildUser))
			{
				registeredDiscordUsers.Add((dbUser, guildUser));
			}
		}

		IEnumerable<uint> lbIdsToRequest = registeredDiscordUsers.Select(ru => (uint)ru.ddUser.LeaderboardId);
		IReadOnlyList<EntryResponse> lbPlayers = await _webService.GetLbPlayers(lbIdsToRequest);

		Dictionary<uint, EntryResponse> lbPlayerLookup = lbPlayers.ToDictionary(lbp => (uint)lbp.Id);

		List<(IGuildUser guildUser, EntryResponse lbPlayer)> registeredDiscordLbPlayers = [];
		foreach ((DdUser ddUser, IGuildUser guildUser) in registeredDiscordUsers)
		{
			if (lbPlayerLookup.TryGetValue((uint)ddUser.LeaderboardId, out EntryResponse? lbPlayer))
			{
				registeredDiscordLbPlayers.Add((guildUser, lbPlayer));
			}
		}

		List<UserRoleUpdate> roleUpdates = [];
		foreach ((IGuildUser guildUser, EntryResponse lbPlayer) in registeredDiscordLbPlayers)
		{
			RoleChangeResult roleChangeResult = GetRoleChange(guildUser.RoleIds, lbPlayer);
			if (roleChangeResult is RoleUpdate roleUpdate)
			{
				roleUpdates.Add(new UserRoleUpdate(guildUser, roleUpdate));
			}
		}

		int nonMemberCount = registeredUserCount - registeredDiscordUsers.Count;
		return new BulkUserRoleUpdates(nonMemberCount, roleUpdates);
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
				ClubberException => ex.Message,
				HttpRequestException => "Couldn't fetch player data. Ddinfo may be down.",
				SerializationException => "Couldn't read player data.",
				_ => "Internal error.",
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
		if (rankRoleToKeep != 0)
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
