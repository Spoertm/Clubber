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
    private readonly AppConfig _appConfig = config.Value;

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

    public async Task<BulkUserRoleUpdates> GetBulkUserRoleUpdates(IReadOnlyCollection<IGuildUser> guildUsers, HashSet<uint> formerWrPlayerIds)
    {
        ImmutableSortedDictionary<int, ulong> scoreRoles = await roleConfigService.GetScoreRolesAsync();
        ImmutableSortedDictionary<int, ulong> rankRoles = await roleConfigService.GetRankRolesAsync();
        RoleContext roleContext = new(scoreRoles, rankRoles, _appConfig.BaseRoles);

        List<DdUser> dbUsers = await userRepository.GetByDiscordIdsAsync(guildUsers.Select(gu => gu.Id));

        Dictionary<ulong, IGuildUser> guildUserById = guildUsers.ToDictionary(gu => gu.Id);

        var registeredUsers = dbUsers
            .Select(ddu => new { DdUser = ddu, GuildUser = guildUserById.GetValueOrDefault(ddu.DiscordId) })
            .Where(x => x.GuildUser != null)
            .ToList();

        uint[] lbIdsToRequest = [.. registeredUsers.Select(ru => ru.DdUser.LeaderboardId).Distinct()];
        Task<IReadOnlyList<EntryResponse>> lbTask = webService.GetLbPlayers(lbIdsToRequest);
        Task<int> countTask = userRepository.GetCountAsync();

        await Task.WhenAll(lbTask, countTask);

        Dictionary<uint, EntryResponse> lbPlayerById = lbTask.Result.ToDictionary(lbp => lbp.Id);
        int totalRegistered = countTask.Result;

        List<UserRoleUpdate> roleUpdates = [];
        foreach (var ru in registeredUsers)
        {
            if (!lbPlayerById.TryGetValue(ru.DdUser.LeaderboardId, out EntryResponse? lbPlayer))
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
            DdUser? ddUser = await userRepository.FindAsync(user.Id);
            if (ddUser is null)
                return Result.Failure<RoleChange>("User is not registered.");

            uint lbId = ddUser.LeaderboardId;

            Task<IReadOnlyList<EntryResponse>> lbPlayerTask = webService.GetLbPlayers([lbId]);
            Task<GetWorldRecordDataContainer> wrTask = webService.GetWorldRecords();
            Task<ImmutableSortedDictionary<int, ulong>> scoreRolesTask = roleConfigService.GetScoreRolesAsync();
            Task<ImmutableSortedDictionary<int, ulong>> rankRolesTask = roleConfigService.GetRankRolesAsync();

            await Task.WhenAll(lbPlayerTask, wrTask, scoreRolesTask, rankRolesTask);

            IReadOnlyList<EntryResponse> lbPlayerList = lbPlayerTask.Result;
            if (lbPlayerList.Count == 0)
            {
                return Result.Failure<RoleChange>("Player not found on leaderboard.");
            }

            HashSet<uint> formerWrPlayerIds = [.. wrTask.Result.WorldRecordHolders.Select(wrh => wrh.Id)];
            RoleContext roleContext = new(scoreRolesTask.Result, rankRolesTask.Result, _appConfig.BaseRoles);

            HashSet<ulong> allPossibleRoles = [.. _appConfig.BaseRoles, .. roleContext.AllPossibleRoles];
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

    private RoleChange GetRoleChange(
        IReadOnlyCollection<ulong> userRoleIds,
        EntryResponse lbUser,
        HashSet<uint> formerWrPlayerIds,
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
            rolesToKeep.Add(_appConfig.FormerWrRoleId);
        }

        CollectionChange<ulong> collectionChange = CollectionUtils.DetermineCollectionChanges(userRoleIds, roleContext.AllPossibleRoles, rolesToKeep);

        if (collectionChange.ItemsToAdd.Count == 0 && collectionChange.ItemsToRemove.Count == 0)
        {
            MilestoneInfo<ulong> milestoneInfo = CollectionUtils.GetNextMileStone(lbUser.Time, roleContext.ScoreRoles);
            return RoleChange.None(milestoneInfo.TimeUntilNextMilestone, milestoneInfo.NextMilestoneId);
        }

        return new RoleChange(collectionChange.ItemsToAdd, collectionChange.ItemsToRemove);
    }

    private record RoleContext(
        ImmutableSortedDictionary<int, ulong> ScoreRoles,
        ImmutableSortedDictionary<int, ulong> RankRoles,
        IReadOnlyCollection<ulong> BaseRoles)
    {
        public HashSet<ulong> AllPossibleRoles { get; } = [.. ScoreRoles.Values, .. RankRoles.Values, .. BaseRoles];
    }
}
