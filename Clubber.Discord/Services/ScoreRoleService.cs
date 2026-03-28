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
    private readonly IReadOnlyCollection<ulong> _baseRoles = GetBaseRoles(config.Value);

    // Assumes scoreRoles is sorted descending by key so the first match is the highest earned threshold.
    public static KeyValuePair<int, ulong> GetScoreRoleToKeep(int playerTime, ImmutableSortedDictionary<int, ulong> scoreRoles)
    {
        int playerTimeSeconds = playerTime / 10_000;
        return scoreRoles
            .Where(r => playerTimeSeconds >= r.Key)
            .FirstOrDefault(scoreRoles.Last());
    }

    public static KeyValuePair<int, ulong> GetRankRoleToKeep(int playerRank, ImmutableSortedDictionary<int, ulong> rankRoles)
    {
        return rankRoles.FirstOrDefault(r => playerRank <= r.Key);
    }


    public async Task<BulkUserRoleUpdates> GetBulkUserRoleUpdates(IReadOnlyCollection<IGuildUser> guildUsers, HashSet<int> formerWrPlayerIds)
    {
        ImmutableSortedDictionary<int, ulong> scoreRoles = await _roleConfigService.GetScoreRolesAsync();
        ImmutableSortedDictionary<int, ulong> rankRoles = await _roleConfigService.GetRankRolesAsync();
        RoleContext roleContext = new(scoreRoles, rankRoles, _baseRoles);

        List<DdUser> dbUsers = await _userRepository.GetByDiscordIdsAsync(guildUsers.Select(gu => gu.Id));

        Dictionary<ulong, IGuildUser> guildUserById = guildUsers.ToDictionary(gu => gu.Id);

        var registeredUsers = dbUsers
            .Select(ddu => new { DdUser = ddu, GuildUser = guildUserById.GetValueOrDefault(ddu.DiscordId) })
            .Where(x => x.GuildUser != null)
            .ToList();

        uint[] lbIdsToRequest = [.. registeredUsers.Select(ru => (uint)ru.DdUser.LeaderboardId).Distinct()];
        Task<IReadOnlyList<EntryResponse>> lbTask = _webService.GetLbPlayers(lbIdsToRequest);
        Task<int> countTask = _userRepository.GetCountAsync();

        await Task.WhenAll(lbTask, countTask);

        Dictionary<uint, EntryResponse> lbPlayerById = lbTask.Result.ToDictionary(lbp => (uint)lbp.Id);
        int totalRegistered = countTask.Result;

        List<UserRoleUpdate> roleUpdates = [];
        foreach (var ru in registeredUsers)
        {
            if (!lbPlayerById.TryGetValue((uint)ru.DdUser.LeaderboardId, out EntryResponse? lbPlayer))
                continue;

            RoleChange change = GetRoleChange(ru.GuildUser!.RoleIds, lbPlayer, formerWrPlayerIds, roleContext);
            if (change.HasChanges)
                roleUpdates.Add(new UserRoleUpdate(ru.GuildUser, change));
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
                return Result.Failure<RoleChange>("User is not registered.");

            uint lbId = (uint)ddUser.LeaderboardId;

            Task<IReadOnlyList<EntryResponse>> lbPlayerTask = _webService.GetLbPlayers([lbId]);
            Task<GetWorldRecordDataContainer> wrTask = _webService.GetWorldRecords();
            Task<ImmutableSortedDictionary<int, ulong>> scoreRolesTask = _roleConfigService.GetScoreRolesAsync();
            Task<ImmutableSortedDictionary<int, ulong>> rankRolesTask = _roleConfigService.GetRankRolesAsync();

            await Task.WhenAll(lbPlayerTask, wrTask, scoreRolesTask, rankRolesTask);

            IReadOnlyList<EntryResponse> lbPlayerList = lbPlayerTask.Result;
            if (lbPlayerList.Count == 0)
            {
                return Result.Failure<RoleChange>("Player not found on leaderboard.");
            }

            HashSet<int> formerWrPlayerIds = [.. wrTask.Result.WorldRecordHolders.Select(wrh => wrh.Id)];
            RoleContext roleContext = new(scoreRolesTask.Result, rankRolesTask.Result, _baseRoles);

            HashSet<ulong> allPossibleRoles = [.. _baseRoles, .. roleContext.AllPossibleRoles];
            RoleChange change = GetRoleChange(user.RoleIds, lbPlayerList[0], formerWrPlayerIds, roleContext);
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
        RoleContext roleContext)
    {
        ulong scoreRoleToKeep = GetScoreRoleToKeep(lbUser.Time, roleContext.ScoreRoles).Value;
        ulong rankRoleToKeep = GetRankRoleToKeep(lbUser.Rank, roleContext.RankRoles).Value;

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

        CollectionChange<ulong> collectionChange = CollectionUtils.DetermineCollectionChanges(userRoleIds, roleContext.AllPossibleRoles, rolesToKeep);

        if (collectionChange.ItemsToAdd.Count == 0 && collectionChange.ItemsToRemove.Count == 0)
        {
            MilestoneInfo<ulong> milestoneInfo = CollectionUtils.GetNextMileStone(lbUser.Time, roleContext.ScoreRoles);
            return RoleChange.None(milestoneInfo.TimeUntilNextMilestone, milestoneInfo.NextMilestoneId);
        }

        return new RoleChange(collectionChange.ItemsToAdd, collectionChange.ItemsToRemove);
    }

    private static IReadOnlyCollection<ulong> GetBaseRoles(AppConfig cfg)
    {
        IReadOnlyCollection<ulong> uselessRoles = [
            cfg.UnregisteredRoleId,
            458375331468935178, // No score
            994354086646399066  // Pending PB
        ];

        return [AppConfig.FormerWrRoleId, .. uselessRoles];
    }

    private record RoleContext(
        ImmutableSortedDictionary<int, ulong> ScoreRoles,
        ImmutableSortedDictionary<int, ulong> RankRoles,
        IReadOnlyCollection<ulong> BaseRoles)
    {
        public HashSet<ulong> AllPossibleRoles { get; } = [.. ScoreRoles.Values, .. RankRoles.Values, .. BaseRoles];
    }
}
