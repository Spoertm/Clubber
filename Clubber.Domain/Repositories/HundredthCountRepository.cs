using Clubber.Domain.Data;
using Clubber.Domain.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Domain.Repositories;

public sealed class HundredthCountRepository(AppDbContext dbContext) : IHundredthCountRepository
{
    public async Task<int> GetCountAsync(int threshold)
    {
        HundredthCount? count = await dbContext.HundredthCounts.AsNoTracking().FirstOrDefaultAsync(hc => hc.Threshold == threshold);
        return count?.Count ?? 0;
    }

    public async Task IncrementAsync(int threshold)
    {
        HundredthCount? existing = await dbContext.HundredthCounts.FindAsync(threshold);
        if (existing is null)
        {
            dbContext.HundredthCounts.Add(new HundredthCount { Threshold = threshold, Count = 1 });
        }
        else
        {
            existing.Count++;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task SeedFromPlayerPbsAsync()
    {
        List<HundredthCount> counts = await dbContext.PlayerPbs
            .Where(p => p.Time >= 1000 * 10_000)
            .GroupBy(p => p.Time / 1_000_000)
            .Select(g => new HundredthCount { Threshold = g.Key * 100, Count = g.Count() })
            .ToListAsync();

        if (counts.Count != 0)
        {
            await dbContext.HundredthCounts.AddRangeAsync(counts);
            await dbContext.SaveChangesAsync();
        }
    }
}
