using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.Data.Entities;
using Clubber.Domain.Models;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Models.Responses.DdInfo;
using Clubber.Domain.Repositories;
using Clubber.Domain.Services;
using Discord;

namespace Clubber.Discord.Services;

/// <summary>Shared `stats` flow: resolves the registered player and builds the stats embed.</summary>
public sealed class UserStatsService(IUserRepository userRepository, UserService userService, IWebService webService)
{
    public async Task<Result<StatsOutcome>> GetStatsAsync(IGuildUser user, bool requestingUserIsStatsUser, bool full)
    {
        DdUser? ddUser = await userRepository.FindAsync(user.Id);
        if (ddUser is null)
        {
            Result validation = await userService.IsValid(user, requestingUserIsStatsUser);
            if (validation is Failure validationFailure)
            {
                return validationFailure;
            }

            return Result.Failure<StatsOutcome>("That user isn't registered.");
        }

        uint leaderboardId = ddUser.LeaderboardId;
        Task<IReadOnlyList<EntryResponse>> playerEntryTask = webService.GetLbPlayers([leaderboardId]);
        Task<GetPlayerHistory?> playerHistoryTask = webService.GetPlayerHistory(leaderboardId);
        await Task.WhenAll(playerEntryTask, playerHistoryTask);

        EntryResponse playerEntry = (await playerEntryTask)[0];
        GetPlayerHistory? playerHistory = await playerHistoryTask;

        if (full)
        {
            return Result.Success(new StatsOutcome(EmbedHelper.FullStats(playerEntry, user, playerHistory), null));
        }

        ComponentBuilder cb = new();
        cb.WithButton("Full stats", $"stats:{user.Id}:{leaderboardId}");
        return Result.Success(new StatsOutcome(EmbedHelper.Stats(playerEntry, user, playerHistory), cb.Build()));
    }
}
