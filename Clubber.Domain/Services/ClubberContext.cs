using Clubber.Domain.Models;
using Clubber.Domain.Models.DdSplits;
using Clubber.Domain.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Clubber.Domain.Services;

public class ClubberContext : DbContext
{
	public DbSet<EntryResponse> LeaderboardCache => Set<EntryResponse>();
	public DbSet<DdUser> DdPlayers => Set<DdUser>();
	public DbSet<DdNewsItem> DdNews => Set<DdNewsItem>();
	public DbSet<BestSplit> BestSplits => Set<BestSplit>();
	public DbSet<HomingPeakRun> TopHomingPeaks => Set<HomingPeakRun>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		IConfigurationBuilder configBuilder = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory());

		string? enrivonmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
		if (enrivonmentName == "Development")
		{
			configBuilder.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
		}
		else
		{
			configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
		}

		IConfigurationRoot configuration = configBuilder.Build();
		string connectionString = configuration.GetConnectionString("DefaultConnection") ??
								throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

		optionsBuilder.UseNpgsql(connectionString);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema("clubber");

		modelBuilder.Entity<BestSplit>().HasKey(bs => bs.Name);

		modelBuilder.Entity<EntryResponse>().HasKey(lbu => lbu.Id);
		modelBuilder.Entity<DdUser>().HasKey(ddu => ddu.LeaderboardId);

		modelBuilder.Entity<DdNewsItem>().HasKey(dni => dni.ItemId);
		modelBuilder.Entity<DdNewsItem>().Property(dni => dni.ItemId).UseIdentityAlwaysColumn();
		modelBuilder.Entity<DdNewsItem>()
			.Property(x => x.TimeOfOccurenceUtc)
			.HasConversion(
				dto => dto.UtcDateTime,
				dt => new(DateTime.SpecifyKind(dt, DateTimeKind.Utc), TimeSpan.Zero));

		modelBuilder.Entity<HomingPeakRun>().Property(hpr => hpr.Id).ValueGeneratedOnAdd();

		modelBuilder.Entity<GameInfo>()
			.Property(g => g.TimeStamp)
			.HasConversion(
				dt => dt.ToUniversalTime(), // convert to UTC when saving to DB
				dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc));
	}
}
