using System.Collections.Immutable;
using Clubber.Domain.Data;
using Clubber.Domain.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Clubber.Domain.Services;

public sealed class RoleConfigService(AppDbContext dbContext, IMemoryCache cache)
{
    private const string ScoreRolesCacheKey = "ScoreRoles";
    private const string RankRolesCacheKey = "RankRoles";
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

    public async Task<ImmutableSortedDictionary<int, ulong>> GetScoreRolesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(ScoreRolesCacheKey, out ImmutableSortedDictionary<int, ulong>? cached))
        {
            return cached!;
        }

        List<ScoreRole> roles = await dbContext.ScoreRoles.AsNoTracking().ToListAsync(ct);
        ImmutableSortedDictionary<int, ulong> sortedDict = roles.ToImmutableSortedDictionary(
            r => r.Id,
            r => r.DiscordRoleId,
            Comparer<int>.Create((x, y) => y.CompareTo(x))); // Descending

        cache.Set(ScoreRolesCacheKey, sortedDict, _cacheDuration);
        return sortedDict;
    }

    public async Task<ImmutableSortedDictionary<int, ulong>> GetRankRolesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(RankRolesCacheKey, out ImmutableSortedDictionary<int, ulong>? cached))
        {
            return cached!;
        }

        List<RankRole> roles = await dbContext.RankRoles.AsNoTracking().ToListAsync(ct);
        ImmutableSortedDictionary<int, ulong> sortedDict = roles.ToImmutableSortedDictionary(r => r.Id, r => r.DiscordRoleId);

        cache.Set(RankRolesCacheKey, sortedDict, _cacheDuration);
        return sortedDict;
    }

    public void InvalidateCache()
    {
        cache.Remove(ScoreRolesCacheKey);
        cache.Remove(RankRolesCacheKey);
    }
}
