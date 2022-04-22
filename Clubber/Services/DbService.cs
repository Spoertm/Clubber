using Clubber.Models;
using Clubber.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Services;

public class DbService : DbContext
{
	public DbSet<EntryResponse> LeaderboardCache => Set<EntryResponse>();
	public DbSet<DdUser> DdPlayers => Set<DdUser>();
	public DbSet<DdNewsItem> DdNews => Set<DdNewsItem>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder
			.UseNpgsql(Environment.GetEnvironmentVariable("PostgresConnectionString")!);

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<DdNewsItem>().Property(ddni => ddni.OldEntry).HasColumnType("jsonb");
		modelBuilder.Entity<DdNewsItem>().Property(ddni => ddni.NewEntry).HasColumnType("jsonb");

		modelBuilder.Entity<EntryResponse>().HasKey(lbu => lbu.Id);
		modelBuilder.Entity<DdUser>().HasKey(ddu => ddu.LeaderboardId);

		modelBuilder.Entity<DdNewsItem>().HasKey(dni => dni.ItemId);
		modelBuilder.Entity<DdNewsItem>().Property(dni => dni.ItemId).UseIdentityAlwaysColumn();
	}
}
