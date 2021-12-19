using Clubber.Models;
using Clubber.Models.Responses;
using Microsoft.EntityFrameworkCore;
using System;

namespace Clubber.Services;

public class DatabaseService : DbContext
{
	public DbSet<EntryResponse> LeaderboardCache { get; set; } = null!;
	public DbSet<DdUser> DdPlayers { get; set; } = null!;

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
