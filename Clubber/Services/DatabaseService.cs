using Clubber.Models;
using Clubber.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Clubber.Services;

public class DatabaseService : DbContext
{
	private readonly IConfiguration _config;

	public DatabaseService(IConfiguration config) => _config = config;

	public DbSet<EntryResponse> LeaderboardCache { get; set; } = null!;
	public DbSet<DdUser> DdPlayers { get; set; } = null!;

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder
			.UseNpgsql(_config["PostgresConnectionString"])
			.UseSnakeCaseNamingConvention();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<EntryResponse>().HasKey(lbu => lbu.Id);
		modelBuilder.Entity<DdUser>().HasKey(ddu => ddu.LeaderboardId);
	}
}
