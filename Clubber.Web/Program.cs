using Clubber.Discord.Helpers;
using Clubber.Discord.Logging;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Clubber.Web.Configuration;
using Clubber.Web.Endpoints;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using System.Globalization;

namespace Clubber.Web;

internal static class Program
{
	public static async Task Main(string[] args)
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
#pragma warning disable CS0618 // Type or member is obsolete
		NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson([typeof(EntryResponse), typeof(GameInfo)]);
#pragma warning restore CS0618 // Type or member is obsolete

		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		builder.ConfigureConfiguration();

		ulong clubberLoggerId = builder.Configuration.GetValue<ulong>("ClubberLoggerId");
		string clubberLoggerToken = builder.Configuration.GetValue<string>("ClubberLoggerToken")!;
		builder.ConfigureLogging(clubberLoggerId, clubberLoggerToken);

		Log.Information("Starting Clubber Web Application");

		// Add MVC and Razor Pages support
		builder.Services.AddControllersWithViews();
		builder.Services.AddEndpointsApiExplorer();

		// Add CORS for API
		builder.Services.AddCors(options =>
		{
			options.AddDefaultPolicy(policy =>
			{
				policy.AllowAnyOrigin()
					.AllowAnyHeader()
					.AllowAnyMethod();
			});
		});

		// Discord Bot Services
		builder.Services.AddSingleton<ClubberDiscordClient>();
		builder.Services.AddSingleton<InteractionHandler>();
		builder.Services.AddSingleton<RegistrationTracker>();

		builder.Services.AddTransient<ScoreRoleService>();
		builder.Services.AddTransient<IDiscordHelper, DiscordHelper>();
		builder.Services.AddTransient<IDatabaseHelper, DatabaseHelper>();
		builder.Services.AddTransient<UserService>();
		builder.Services.AddTransient<IWebService, WebService>();

		builder.Services.AddHttpClient();
		builder.Services.AddDbContext<DbService>(ServiceLifetime.Transient);

		// Production services
		if (builder.Environment.IsProduction())
		{
			builder.Services.AddSingleton<UserJoinHandler>();
			builder.Services.AddSingleton<RegistrationRequestHandler>();
			builder.Services.AddHostedService<DdNewsPostService>();
			builder.Services.AddHostedService<DatabaseUpdateService>();
			builder.Services.AddHostedService<KeepAppAliveService>();
			builder.Services.AddHostedService<ChannelClearingService>();
		}

		// Swagger/OpenAPI
		builder.Services.AddSwaggerGen(options =>
		{
			options.EnableAnnotations();
			options.SwaggerDoc("v1", new OpenApiInfo
			{
				Version = "v1",
				Title = "Clubber API",
				Description = """
				              RESTful API for the Clubber Discord bot. Access Devil Daggers community data including
				              registered users, daily news, best splits, and more. Perfect for building integrations
				              and custom applications for the DD community.
				              """,
				Contact = new OpenApiContact
				{
					Name = "Spoertm",
					Url = new Uri("https://github.com/Spoertm")
				},
				License = new OpenApiLicense
				{
					Name = "View on GitHub",
					Url = new Uri("https://github.com/Spoertm/Clubber")
				}
			});
		});

		WebApplication app = builder.Build();

		// Configure request pipeline
		if (app.Environment.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		else
		{
			app.UseExceptionHandler("/Home/Error");
			app.UseHsts();
		}

		app.UseHttpsRedirection();
		app.UseStaticFiles();

		app.UseRouting();
		app.UseCors();

		// Swagger UI
		app.UseSwagger();
		app.UseSwaggerUI(options =>
		{
			options.SwaggerEndpoint("/swagger/v1/swagger.json", "Main");
			options.RoutePrefix = "swagger";
			options.DocumentTitle = "Clubber API Documentation";
		});

		// Map MVC routes FIRST
		app.MapControllerRoute(
			"default",
			"{controller=Home}/{action=Index}/{id?}");

		app.RegisterClubberEndpoints();

		try
		{
			app.Services.GetRequiredService<InteractionHandler>();

			if (app.Environment.IsProduction())
			{
				app.Services.GetRequiredService<RegistrationRequestHandler>();
				app.Services.GetRequiredService<UserJoinHandler>();
			}

			await app.Services.GetRequiredService<ClubberDiscordClient>().InitAsync();

			Log.Information("Clubber Web Application started successfully");
			await app.RunAsync();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Fatal error in Clubber Web Application");
		}
		finally
		{
			Log.Information("Clubber Web Application shutdown complete");
			await Log.CloseAndFlushAsync();
		}
	}

	private static void ConfigureLogging(this WebApplicationBuilder builder, ulong clubberLoggerId, string clubberLoggerToken)
	{
		builder.Logging.ClearProviders();

		Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.MinimumLevel.Information()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u4}] {Message:lj}{NewLine}{Exception}",
				formatProvider: CultureInfo.InvariantCulture)
			.WriteTo.Discord(clubberLoggerId, clubberLoggerToken)
			.CreateLogger();

		builder.Host.UseSerilog();
	}
}
