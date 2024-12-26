using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Services;
using Clubber.Web.Server.Endpoints;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;

namespace Clubber.Web.Server;

internal static class Program
{
	public static async Task Main(string[] args)
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		builder.ConfigureConfiguration();
		builder.ConfigureLogging();

		Log.Information("Starting");

		builder.Services.AddRazorPages();
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddCors();

		builder.Services.AddSingleton<ClubberDiscordClient>();
		builder.Services.AddSingleton<CommandService>(_ => new(new()
		{
			IgnoreExtraArgs = true,
			DefaultRunMode = RunMode.Async,
		}));

		builder.Services.AddSingleton<MessageHandlerService>();
		builder.Services.AddSingleton<InteractionHandler>();
		builder.Services.AddSingleton<RegistrationTracker>();
		builder.Services.AddSingleton<EmbedHelper>();

		builder.Services.AddTransient<ScoreRoleService>();
		builder.Services.AddTransient<IDiscordHelper, DiscordHelper>();
		builder.Services.AddTransient<IDatabaseHelper, DatabaseHelper>();
		builder.Services.AddTransient<UserService>();
		builder.Services.AddTransient<IWebService, WebService>();

		builder.Services.AddHttpClient();
		builder.Services.AddDbContext<ClubberContext>(ServiceLifetime.Transient);

		if (builder.Environment.IsProduction())
		{
			builder.Services.AddSingleton<UserJoinHandler>();
			builder.Services.AddSingleton<RegistrationRequestHandler>();
			builder.Services.AddHostedService<DdNewsPostService>();
			builder.Services.AddHostedService<DatabaseUpdateService>();
			builder.Services.AddHostedService<KeepAppAliveService>();
		}

		builder.Services.AddSwaggerGen(options =>
		{
			options.EnableAnnotations();
			options.SwaggerDoc("Main", new()
			{
				Version = "Main",
				Title = "Clubber API",
				Description = """
							This is an API for getting information regarding registered users in the DD Pals Discord server.
							Additional information regarding the best Devil Daggers splits and new 1000+ scores can also be obtained.
							""",
			});
		});

		WebApplication app = builder.Build();

		await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
		{
			ClubberContext context = scope.ServiceProvider.GetRequiredService<ClubberContext>();
			await context.Database.MigrateAsync();
		}

		app.RegisterClubberEndpoints();

		if (app.Environment.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
			app.UseWebAssemblyDebugging();
		}

		app.UseStaticFiles();

		app.UseSwagger();

		app.UseSwaggerUI(options =>
		{
			options.InjectStylesheet("/swagger-ui/SwaggerDarkReader.css");
			options.SwaggerEndpoint("/swagger/Main/swagger.json", "Main");
		});

		app.UseBlazorFrameworkFiles();

		app.UseRouting();

		app.MapRazorPages();

		app.MapFallbackToFile("index.html");

		app.UseHttpsRedirection();

		app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin());

		// Initialize services
		if (app.Environment.IsProduction())
		{
			app.Services.GetRequiredService<UserJoinHandler>();
			app.Services.GetRequiredService<RegistrationRequestHandler>();
		}

		app.Services.GetRequiredService<MessageHandlerService>();
		app.Services.GetRequiredService<InteractionHandler>();

		await app.Services.GetRequiredService<ClubberDiscordClient>().InitAsync();

		try
		{
			await app.RunAsync();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Caught error in main application loop");
		}
		finally
		{
			Log.Information("Shut-down complete");
			await Log.CloseAndFlushAsync();
		}
	}

	private static void ConfigureLogging(this WebApplicationBuilder builder)
	{
		builder.Logging.ClearProviders();

		Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.MinimumLevel.Debug()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u4}] {Message:lj}{NewLine}{Exception}", formatProvider: CultureInfo.InvariantCulture)
			.CreateLogger();
	}

	private static void ConfigureConfiguration(this WebApplicationBuilder builder)
	{
		if (builder.Environment.EnvironmentName == "Development")
		{
			builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
		}
		else
		{
			builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
		}

		builder.Services
			.AddOptions<AppConfig>()
			.BindConfiguration("")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		builder.Services.AddOptions<BotConfig>()
			.BindConfiguration("BotConfig")
			.ValidateDataAnnotations()
			.ValidateOnStart();
	}
}
