namespace Clubber.Domain.Repositories;

public interface IAppStateRepository
{
    Task<DateTimeOffset?> GetLastNewsCheckAsync();

    Task SetLastNewsCheckAsync(DateTimeOffset value);
}
