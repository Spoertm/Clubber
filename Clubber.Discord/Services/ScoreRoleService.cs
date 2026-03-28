using System.Collections.Immutable;
using System.Runtime.Serialization;
using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord;
using Microsoft.Extensions.Options;
using Serilog;

namespace Clubber.Discord.Services;

public sealed class ScoreRoleService(
    IOptions<AppConfig> config,
    IWebService webService,
    IUserRepository userRepository,
    RoleConfigService roleConfigService)
{
    private readonly IWebService _webService = webService;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly RoleConfigService _roleConfigService = roleConfigService;
    private readonly IReadOnlyCollection<ulong> _allPossibleRoles = GetAllPossibleRoles(config.Value);

    private static IReadOnlyCollection<ulong> GetAllPossibleRoles(AppConfig cfg)
    {
        IReadOnlyCollection<ulong> uselessRoles = [
            cfg.UnregisteredRoleId,
            458375331468935178, // No score
            994354086646399066 // Pending PB
        ];

        return [AppConfig.FormerWrRoleId, .. uselessRoles];
    }

    public async Task<BulkUserRoleUpdates> GetBulkUserRoleUpdates(IReadOnlyCollection<IGuildUser> guildUsers, HashSet<int> formerWrPlayerIds)
    {
        // Load roles once for this operation
        ImmutableSortedDictionary<int, ulong> scoreRoles = await _roleConfigService.GetScoreRolesAsync();
        ImmutableSortedDictionary<int, ulong> rankRoles = await _roleConfigService.GetRankRolesAsync();

        // Single DB call to get registered users
        List<DdUser> dbUsers = await _userRepository.GetByDiscordIdsAsync(guildUsers.Select(gu => gu.Id));

        // Build a dictionary for O(1) lookup
        Dictionary<ulong, IGuildUser> guildUserById = guildUsers.ToDictionary(gu => gu.Id);

        // Join in memory using dictionary
        var registeredUsers = dbUsers
            .Select(ddu => new { DdUser = ddu, GuildUser = guildUserById.GetValueOrDefault(ddu.DiscordId) })
            .Where(x => x.GuildUser != null)
            .ToList();

        // Fetch leaderboard data and total count in parallel
        uint[] lbIdsToRequest = [.. registeredUsers.Select(ru => (uint)ru.DdUser.LeaderboardId).Distinct()];
        Task<IReadOnlyList<EntryResponse>> lbTask = _webService.GetLbPlayers(lbIdsToRequest);
        Task<int> countTask = _userRepository.GetCountAsync();

        await Task.WhenAll(lbTask, countTask);

        IReadOnlyList<EntryResponse> lbPlayers = await lbTask;
        int totalRegistered = await countTask;

        // Build dictionary for leaderboard players
        Dictionary<uint, EntryResponse> lbPlayerById = lbPlayers.ToDictionary(lbp => (uint)lbp.Id);

        // Calculate role changes
        HashSet<ulong> allPossibleRoles = [.. _allPossibleRoles, .. scoreRoles.Values, .. rankRoles.Values];
        List<UserRoleUpdate> roleUpdates = [];
        foreach (var ru in registeredUsers)
        {
            if (!lbPlayerById.TryGetValue((uint)ru.DdUser.LeaderboardId, out EntryResponse? lbPlayer))
                continue;

            RoleChange change = GetRoleChange(ru.GuildUser!.RoleIds, lbPlayer, formerWrPlayerIds, scoreRoles, rankRoles, allPossibleRoles);
            if (change.HasChanges)
            {
                roleUpdates.Add(new UserRoleUpdate(ru.GuildUser, change));
            }
        }

        int nonMemberCount = totalRegistered - registeredUsers.Count;
        return new BulkUserRoleUpdates(nonMemberCount, roleUpdates);
    }

    public async Task<Result<RoleChange>> GetRoleChange(IGuildUser user)
    {
        try
        {
            DdUser? ddUser = await _userRepository.FindAsync(user.Id);
            if (ddUser is null)
            {
                return Result.Failure<RoleChange>("User is not registered.");
            }

            uint lbId = (uint)ddUser.LeaderboardId;

            Task<IReadOnlyList<EntryResponse>> lbPlayerTask = _webService.GetLbPlayers([lbId]);
            Task<GetWorldRecordDataContainer> wrTask = _webService.GetWorldRecords();
            Task<ImmutableSortedDictionary<int, ulong>> scoreRolesTask = _roleConfigService.GetScoreRolesAsync();
            Task<ImmutableSortedDictionary<int, ulong>> rankRolesTask = _roleConfigService.GetRankRolesAsync();

            await Task.WhenAll(lbPlayerTask, wrTask, scoreRolesTask, rankRolesTask);

            IReadOnlyList<EntryResponse> lbPlayerList = await lbPlayerTask;
            if (lbPlayerList.Count == 0)
            {
                return Result.Failure<RoleChange>("Player not found on leaderboard.");
            }

            HashSet<int> formerWrPlayerIds = [.. (await wrTask).WorldRecordHolders.Select(wrh => wrh.Id)];
            ImmutableSortedDictionary<int, ulong> scoreRoles = await scoreRolesTask;
            ImmutableSortedDictionary<int, ulong> rankRoles = await rankRolesTask;

            HashSet<ulong> allPossibleRoles = [.. _allPossibleRoles, .. scoreRoles.Values, .. rankRoles.Values];
            RoleChange change = GetRoleChange(user.RoleIds, lbPlayerList[0], formerWrPlayerIds, scoreRoles, rankRoles, allPossibleRoles);
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

    private static RoleChange GetRoleChange(
        IReadOnlyCollection<ulong> userRoleIds,
        EntryResponse lbUser,
        HashSet<int> formerWrPlayerIds,
        ImmutableSortedDictionary<int, ulong> scoreRoles,
        ImmutableSortedDictionary<int, ulong> rankRoles,
        HashSet<ulong> allPossibleRoles)
    {
        ulong scoreRoleToKeep = GetScoreRoleToKeep(lbUser.Time, scoreRoles).Value;
        ulong rankRoleToKeep = GetRankRoleToKeep(lbUser.Rank, rankRoles).Value;

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

        CollectionChange<ulong> collectionChange = CollectionUtils.DetermineCollectionChanges(userRoleIds, allPossibleRoles, rolesToKeep);

        if (collectionChange.ItemsToAdd.Count == 0 && collectionChange.ItemsToRemove.Count == 0)
        {
            MilestoneInfo<ulong> milestoneInfo = CollectionUtils.GetNextMileStone(lbUser.Time, scoreRoles);
            return RoleChange.None(milestoneInfo.TimeUntilNextMilestone, milestoneInfo.NextMilestoneId);
        }

        return new RoleChange(collectionChange.ItemsToAdd, collectionChange.ItemsToRemove);
    }

    public static KeyValuePair<int, ulong> GetScoreRoleToKeep(int playerTime, ImmutableSortedDictionary<int, ulong> scoreRoles)
    {
        // Score is stored in tenths of seconds, convert to seconds for comparison
        int playerTimeSeconds = playerTime / 10_000;
        return scoreRoles
            .Where(r => playerTimeSeconds >= r.Key)
            .FirstOrDefault(scoreRoles.Last());
    }

    public static KeyValuePair<int, ulong> GetRankRoleToKeep(int playerRank, ImmutableSortedDictionary<int, ulong> rankRoles)
    {
        return rankRoles.FirstOrDefault(r => playerRank <= r.Key);
    }
}
