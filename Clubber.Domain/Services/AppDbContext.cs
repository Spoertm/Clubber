using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clubber.Domain.Services;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<EntryResponse> LeaderboardCache => Set<EntryResponse>();

    public DbSet<DdUser> DdPlayers => Set<DdUser>();

    public DbSet<DdNewsItem> DdNews => Set<DdNewsItem>();

    public DbSet<BestSplit> BestSplits => Set<BestSplit>();

    public DbSet<HomingPeakRun> TopHomingPeaks => Set<HomingPeakRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("clubber");

        modelBuilder.Entity<EntryResponse>().HasKey(lbu => lbu.Id);
        modelBuilder.Entity<DdUser>().HasKey(ddu => ddu.LeaderboardId);

        modelBuilder.Entity<DdNewsItem>().HasKey(dni => dni.ItemId);
        modelBuilder.Entity<DdNewsItem>().Property(dni => dni.ItemId)
            .ValueGeneratedOnAdd()
            .UseIdentityAlwaysColumn();

        modelBuilder.Entity<HomingPeakRun>().Property(hpr => hpr.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<DdNewsItem>().ComplexProperty(dni => dni.OldEntry);
        modelBuilder.Entity<DdNewsItem>().ComplexProperty(dni => dni.NewEntry);
        modelBuilder.Entity<BestSplit>().ComplexProperty(bs => bs.GameInfo, g => g.IsRequired(false));

        // Configure DateTimeOffset converter for SQLite compatibility
        ValueConverter<DateTimeOffset, DateTime> dateTimeOffsetConverter = new(
            v => v.UtcDateTime,
            v => new DateTimeOffset(v, TimeSpan.Zero));

        modelBuilder.Entity<DdNewsItem>().Property(e => e.TimeOfOccurenceUtc).HasConversion(dateTimeOffsetConverter);
    }
}
