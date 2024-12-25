using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Domain.Services;

public class ClubberContext : DbContext
{
	public DbSet<EntryResponse> LeaderboardCache => Set<EntryResponse>();
	public DbSet<DdUser> DdPlayers => Set<DdUser>();
	public DbSet<DdNewsItem> DdNews => Set<DdNewsItem>();
	public DbSet<BestSplit> BestSplits => Set<BestSplit>();
	public DbSet<HomingPeakRun> TopHomingPeaks => Set<HomingPeakRun>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<DdNewsItem>().Property(ddni => ddni.OldEntry).HasColumnType("jsonb");
		modelBuilder.Entity<DdNewsItem>().Property(ddni => ddni.NewEntry).HasColumnType("jsonb");

		modelBuilder.Entity<EntryResponse>().HasKey(lbu => lbu.Id);
		modelBuilder.Entity<DdUser>().HasKey(ddu => ddu.LeaderboardId);

		modelBuilder.Entity<DdNewsItem>().HasKey(dni => dni.ItemId);
		modelBuilder.Entity<DdNewsItem>().Property(dni => dni.ItemId).UseIdentityAlwaysColumn();

		modelBuilder.Entity<HomingPeakRun>().Property(hpr => hpr.Id).ValueGeneratedOnAdd();
	}
}
