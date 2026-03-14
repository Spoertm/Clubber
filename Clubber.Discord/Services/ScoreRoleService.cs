using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Services;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Runtime.Serialization;

namespace Clubber.Discord.Services;

public sealed class ScoreRoleService(
	IOptions<AppConfig> config,
	IServiceScopeFactory serviceScopeFactory,
	IWebService webService,
	IDatabaseHelper databaseHelper)
{
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
	private readonly IWebService _webService = webService;
	private readonly IDatabaseHelper _databaseHelper = databaseHelper;
	private readonly IReadOnlyCollection<ulong> _allPossibleRoles = GetAllPossibleRoles(config.Value);

	private static IReadOnlyCollection<ulong> GetAllPossibleRoles(AppConfig cfg)
	{
		IReadOnlyCollection<ulong> uselessRoles = [cfg.UnregisteredRoleId, 458375331468935178, 994354086646399066];
		return [.. AppConfig.ScoreRoles.Values, .. AppConfig.RankRoles.Values, AppConfig.FormerWrRoleId, .. uselessRoles];
	}

	public async Task<BulkUserRoleUpdates> GetBulkUserRoleUpdates(IReadOnlyCollection<IGuildUser> guildUsers)
	{
		// Single DB call to get registered users
		List<DdUser> dbUsers = await _databaseHelper.GetRegisteredUsers(guildUsers.Select(gu => gu.Id));

		// Build a dictionary for O(1) lookup
		Dictionary<ulong, IGuildUser> guildUserById = guildUsers.ToDictionary(gu => gu.Id);

		// Join in memory using dictionary
		var registeredUsers = dbUsers
			.Select(ddu => new { DdUser = ddu, GuildUser = guildUserById.GetValueOrDefault(ddu.DiscordId) })
			.Where(x => x.GuildUser != null)
			.ToList();

		// Fetch leaderboard data
		uint[] lbIdsToRequest = registeredUsers.Select(ru => (uint)ru.DdUser.LeaderboardId).Distinct().ToArray();
		IReadOnlyList<EntryResponse> lbPlayers = await _webService.GetLbPlayers(lbIdsToRequest);

		// Fetch world records
		GetWorldRecordDataContainer worldRecords = await _webService.GetWorldRecords();
		HashSet<int> formerWrPlayerIds = [.. worldRecords.WorldRecordHolders.Select(wrh => wrh.Id)];

		// Build dictionary for leaderboard players
		Dictionary<uint, EntryResponse> lbPlayerById = lbPlayers.ToDictionary(lbp => (uint)lbp.Id);

		// Calculate role changes
		List<UserRoleUpdate> roleUpdates = [];
		foreach (var ru in registeredUsers)
		{
			if (ru.GuildUser == null)
				continue;

			if (!lbPlayerById.TryGetValue((uint)ru.DdUser.LeaderboardId, out EntryResponse? lbPlayer))
				continue;

			RoleChange change = GetRoleChange(ru.GuildUser.RoleIds, lbPlayer, formerWrPlayerIds);
			if (change.HasChanges)
			{
				roleUpdates.Add(new UserRoleUpdate(ru.GuildUser, change));
			}
		}

		int totalRegistered = await _databaseHelper.GetRegisteredUserCount();
		int nonMemberCount = totalRegistered - registeredUsers.Count;
		return new BulkUserRoleUpdates(nonMemberCount, roleUpdates);
	}

	public async Task<Result<RoleChange>> GetRoleChange(IGuildUser user)
	{
		try
		{
			await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
			IDatabaseHelper dbHelper = scope.ServiceProvider.GetRequiredService<IDatabaseHelper>();

			DdUser? ddUser = await dbHelper.FindRegisteredUser(user.Id);
			if (ddUser is null)
			{
				return Result.Failure<RoleChange>("User is not registered.");
			}

			uint lbId = (uint)ddUser.LeaderboardId;
			IReadOnlyList<EntryResponse> lbPlayerList = await _webService.GetLbPlayers([lbId]);

			GetWorldRecordDataContainer worldRecords = await _webService.GetWorldRecords();
			HashSet<int> formerWrPlayerIds = [.. worldRecords.WorldRecordHolders.Select(wrh => wrh.Id)];

			RoleChange change = GetRoleChange(user.RoleIds, lbPlayerList[0], formerWrPlayerIds);
			return Result.Success(change);
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
			return Result.Failure<RoleChange>(errorMsg);
		}
	}

	private RoleChange GetRoleChange(IReadOnlyCollection<ulong> roleIds, EntryResponse lbUser, HashSet<int> formerWrPlayerIds)
	{
		ulong scoreRoleToKeep = GetScoreRoleToKeep(lbUser.Time).Value;
		ulong rankRoleToKeep = GetRankRoleToKeep(lbUser.Rank).Value;

		List<ulong> rolesToKeep = [scoreRoleToKeep];

		bool isCurrentWr = lbUser.Rank == 1;
		bool wasEverWr = formerWrPlayerIds.Contains(lbUser.Id);

		if (rankRoleToKeep != 0)
		{
			rolesToKeep.Add(rankRoleToKeep);
		}

		if (wasEverWr && !isCurrentWr)
		{
			rolesToKeep.Add(AppConfig.FormerWrRoleId);
		}

		CollectionChange<ulong> collectionChange = CollectionUtils.DetermineCollectionChanges(roleIds, _allPossibleRoles, rolesToKeep);

		if (collectionChange.ItemsToAdd.Count == 0 && collectionChange.ItemsToRemove.Count == 0)
		{
			MilestoneInfo<ulong> milestoneInfo = CollectionUtils.GetNextMileStone(lbUser.Time, AppConfig.ScoreRoles);
			return RoleChange.None(milestoneInfo.TimeUntilNextMilestone, milestoneInfo.NextMilestoneId);
		}

		return new RoleChange(collectionChange.ItemsToAdd, collectionChange.ItemsToRemove);
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
