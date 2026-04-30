using Clubber.Domain.Data.Entities;
using Clubber.Domain.Data.Entities.DdSplits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clubber.Domain.Data;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlayerPb> PlayerPbs => Set<PlayerPb>();

    public DbSet<HundredthCount> HundredthCounts => Set<HundredthCount>();

    public DbSet<NewsCursor> NewsCursors => Set<NewsCursor>();

    public DbSet<DdUser> DdPlayers => Set<DdUser>();

    public DbSet<DdNewsItem> DdNews => Set<DdNewsItem>();

    public DbSet<BestSplit> BestSplits => Set<BestSplit>();

    public DbSet<HomingPeakRun> TopHomingPeaks => Set<HomingPeakRun>();

    public DbSet<ScoreRole> ScoreRoles => Set<ScoreRole>();

    public DbSet<RankRole> RankRoles => Set<RankRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("clubber");

        modelBuilder.Entity<PlayerPb>().HasKey(pp => pp.LeaderboardId);
        modelBuilder.Entity<HundredthCount>().HasKey(hc => hc.Threshold);
        modelBuilder.Entity<NewsCursor>().HasKey(nc => nc.Id);
        modelBuilder.Entity<NewsCursor>().Property(nc => nc.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<DdUser>().HasKey(ddu => ddu.DiscordId);
        modelBuilder.Entity<DdUser>().HasIndex(ddu => ddu.LeaderboardId);

        modelBuilder.Entity<DdNewsItem>().HasKey(dni => dni.ItemId);
        modelBuilder.Entity<DdNewsItem>().Property(dni => dni.ItemId)
            .ValueGeneratedOnAdd()
            .UseIdentityAlwaysColumn();

        modelBuilder.Entity<HomingPeakRun>().HasKey(hpr => hpr.Id);
        modelBuilder.Entity<HomingPeakRun>().Property(hpr => hpr.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<BestSplit>().HasKey(bs => bs.Name);

        modelBuilder.Entity<ScoreRole>().HasKey(sr => sr.Id);
        modelBuilder.Entity<ScoreRole>().Property(sr => sr.Id).ValueGeneratedNever();
        modelBuilder.Entity<RankRole>().HasKey(rr => rr.Id);
        modelBuilder.Entity<RankRole>().Property(rr => rr.Id).ValueGeneratedNever();

        modelBuilder.Entity<DdNewsItem>().ComplexProperty(dni => dni.OldEntry);
        modelBuilder.Entity<DdNewsItem>().ComplexProperty(dni => dni.NewEntry);
        modelBuilder.Entity<BestSplit>().ComplexProperty(bs => bs.GameInfo, g => g.IsRequired(false));

        // Configure DateTimeOffset converter for SQLite compatibility
        ValueConverter<DateTimeOffset, DateTime> dateTimeOffsetConverter = new(
            v => v.UtcDateTime,
            v => new DateTimeOffset(v, TimeSpan.Zero));

        modelBuilder.Entity<DdNewsItem>().Property(e => e.TimeOfOccurenceUtc).HasConversion(dateTimeOffsetConverter);
        modelBuilder.Entity<NewsCursor>().Property(e => e.LastCheckedAt).HasConversion(dateTimeOffsetConverter);
        modelBuilder.Entity<PlayerPb>().Property(e => e.LastUpdated).HasConversion(dateTimeOffsetConverter);
    }
}
