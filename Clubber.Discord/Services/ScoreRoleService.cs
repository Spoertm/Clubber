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

public sealed class ScoreRoleService
{
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IWebService _webService;
	private readonly IReadOnlyCollection<ulong> _allPossibleRoles;

	public ScoreRoleService(IOptions<AppConfig> config, IServiceScopeFactory serviceScopeFactory, IWebService webService)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_webService = webService;

		IReadOnlyCollection<ulong> uselessRoles = [config.Value.UnregisteredRoleId, 458375331468935178, 994354086646399066];
		_allPossibleRoles = [..AppConfig.ScoreRoles.Values, ..AppConfig.RankRoles.Values, AppConfig.FormerWrRoleId, ..uselessRoles];
	}

	public async Task<BulkUserRoleUpdates> GetBulkUserRoleUpdates(IReadOnlyCollection<IGuildUser> guildUsers)
	{
		IEnumerable<ulong> guildUserIds = guildUsers.Select(gu => gu.Id);

		Task<int> allUsersTask = GetRegisteredUserCountAsync();
		Task<List<DdUser>> filteredUsersTask = GetRegisteredUsersAsync(guildUserIds);

		await Task.WhenAll(allUsersTask, filteredUsersTask);

		int registeredUserCount = await allUsersTask;
		List<DdUser> dbUsers = await filteredUsersTask;

		(DdUser ddUser, IGuildUser guildUser)[] registeredUsers = dbUsers.Join(
				inner: guildUsers,
				outerKeySelector: dbu => dbu.DiscordId,
				innerKeySelector: gu => gu.Id,
				resultSelector: (ddUser, guildUser) => (ddUser, guildUser))
			.ToArray();

		IEnumerable<uint> lbIdsToRequest = registeredUsers
			.Select(ru => (uint)ru.ddUser.LeaderboardId)
			.Distinct();

		IEnumerable<uint> idsToRequest = lbIdsToRequest as uint[] ?? lbIdsToRequest.ToArray();
		IReadOnlyList<EntryResponse> lbPlayers = await _webService.GetLbPlayers(idsToRequest);

		GetWorldRecordDataContainer worldRecords = await _webService.GetWorldRecords();
		HashSet<int> formerWrPlayerIds = worldRecords.WorldRecordHolders.Select(wrh => wrh.Id).ToHashSet();

		(IGuildUser guildUser, EntryResponse lbPlayer)[] registeredDiscordLbPlayers = registeredUsers.Join(
				inner: lbPlayers,
				outerKeySelector: ru => (uint)ru.ddUser.LeaderboardId,
				innerKeySelector: lbp => (uint)lbp.Id,
				resultSelector: (ru, lbp) => (ru.guildUser, lbp))
			.ToArray();

		List<UserRoleUpdate> roleUpdates = [];
		foreach ((IGuildUser guildUser, EntryResponse lbPlayer) in registeredDiscordLbPlayers)
		{
			RoleChangeResult roleChangeResult = GetRoleChange(guildUser.RoleIds, lbPlayer, formerWrPlayerIds);
			if (roleChangeResult is RoleUpdate roleUpdate)
			{
				roleUpdates.Add(new UserRoleUpdate(guildUser, roleUpdate));
			}
		}

		int nonMemberCount = registeredUserCount - registeredUsers.Length;
		return new BulkUserRoleUpdates(nonMemberCount, roleUpdates);
	}

	private async Task<int> GetRegisteredUserCountAsync()
	{
		await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
		IDatabaseHelper databaseHelper = scope.ServiceProvider.GetRequiredService<IDatabaseHelper>();
		return await databaseHelper.GetRegisteredUserCount();
	}

	private async Task<List<DdUser>> GetRegisteredUsersAsync(IEnumerable<ulong> discordIds)
	{
		await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
		IDatabaseHelper databaseHelper = scope.ServiceProvider.GetRequiredService<IDatabaseHelper>();
		return await databaseHelper.GetRegisteredUsers(discordIds);
	}

	public async Task<Result<RoleChangeResult>> GetRoleChange(IGuildUser user)
	{
		try
		{
			await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
			IDatabaseHelper databaseHelper = scope.ServiceProvider.GetRequiredService<IDatabaseHelper>();

			DdUser? ddUser = await databaseHelper.FindRegisteredUser(user.Id);
			if (ddUser is null)
			{
				return Result.Failure<RoleChangeResult>("User is not registered.")!;
			}

			uint lbId = (uint)ddUser.LeaderboardId;
			IReadOnlyList<EntryResponse> lbPlayerList = await _webService.GetLbPlayers([lbId]);

			GetWorldRecordDataContainer worldRecords = await _webService.GetWorldRecords();
			HashSet<int> formerWrPlayerIds = worldRecords.WorldRecordHolders.Select(wrh => wrh.Id).ToHashSet();

			return Result.Success(GetRoleChange(user.RoleIds, lbPlayerList[0], formerWrPlayerIds));
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

	private RoleChangeResult GetRoleChange(IReadOnlyCollection<ulong> roleIds, EntryResponse lbUser, HashSet<int> formerWrPlayerIds)
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
