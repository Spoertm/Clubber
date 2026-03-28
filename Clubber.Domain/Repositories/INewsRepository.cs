using Clubber.Domain.Data.Entities;

namespace Clubber.Domain.Repositories;

public interface INewsRepository
{
    Task AddAsync(DdNewsItem item);

    Task RemoveOlderThanAsync(TimeSpan age);

    Task<DdNewsItem[]> GetRecentAsync();
}
