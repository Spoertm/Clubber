using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Domain.Repositories;

public sealed class NewsRepository(DbService dbContext) : INewsRepository
{
	public async Task AddAsync(DdNewsItem item)
	{
		await dbContext.DdNews.AddAsync(item);
		await dbContext.SaveChangesAsync();
	}

	public async Task RemoveOlderThanAsync(TimeSpan age)
	{
		DateTimeOffset cutoff = DateTimeOffset.UtcNow - age;
		IQueryable<DdNewsItem> toRemove = dbContext.DdNews.Where(ddn => ddn.TimeOfOccurenceUtc <= cutoff);

		if (await toRemove.AnyAsync())
		{
			dbContext.DdNews.RemoveRange(toRemove);
			await dbContext.SaveChangesAsync();
		}
	}

	public async Task<DdNewsItem[]> GetRecentAsync()
	{
		return await dbContext.DdNews
			.AsNoTracking()
			.OrderByDescending(d => d.TimeOfOccurenceUtc)
			.ToArrayAsync();
	}
}
