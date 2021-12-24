using Clubber.Models;
using Clubber.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Clubber.Services;

public class DatabaseService : DbContext
{
	public DbSet<EntryResponse> LeaderboardCache => Set<EntryResponse>();
	public DbSet<DdUser> DdPlayers => Set<DdUser>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder
			.UseNpgsql(Environment.GetEnvironmentVariable("PostgresConnectionString")!)
			.UseSnakeCaseNamingConvention();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<EntryResponse>().HasKey(lbu => lbu.Id);
		modelBuilder.Entity<DdUser>().HasKey(ddu => ddu.LeaderboardId);
	}
}
