using System.Globalization;
using Clubber.Domain.Data;
using Clubber.Domain.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Domain.Repositories;

public sealed class AppStateRepository(AppDbContext dbContext) : IAppStateRepository
{
    private const string LastNewsCheckKey = "DdNewsLastCheck";

    public async Task<DateTimeOffset?> GetLastNewsCheckAsync()
    {
        AppState? state = await dbContext.AppStates
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Key == LastNewsCheckKey);

        if (state is null || !DateTimeOffset.TryParse(state.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset result))
        {
            return null;
        }

        return result;
    }

    public async Task SetLastNewsCheckAsync(DateTimeOffset value)
    {
        AppState? existing = await dbContext.AppStates.FindAsync(LastNewsCheckKey);
        if (existing is null)
        {
            dbContext.AppStates.Add(new AppState { Key = LastNewsCheckKey, Value = value.ToString("O") });
        }
        else
        {
            existing.Value = value.ToString("O");
        }

        await dbContext.SaveChangesAsync();
    }
}
