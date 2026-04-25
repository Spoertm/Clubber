namespace Clubber.Domain.Repositories;

public interface IHundredthCountRepository
{
    Task<int> GetCountAsync(int threshold);

    Task IncrementAsync(int threshold);

    Task SeedFromPlayerPbsAsync();
}
