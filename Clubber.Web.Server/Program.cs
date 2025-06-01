using Clubber.Discord.Helpers;
using Clubber.Discord.Logging;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Clubber.Web.Server.Configuration;
using Clubber.Web.Server.Endpoints;
using Discord.Commands;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using System.Globalization;

namespace Clubber.Web.Server;

internal static class Program
{
	public static async Task Main(string[] args)
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
#pragma warning disable CS0618 // Type or member is obsolete
		NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson([typeof(EntryResponse), typeof(GameInfo)]);
#pragma warning restore CS0618 // Type or member is obsolete

		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		builder.ConfigureConfiguration();

		AppConfig appConfig = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AppConfig>>().Value;
		builder.ConfigureLogging(appConfig);

		Log.Information("Starting");

		CommandService commands = new(new CommandServiceConfig
		{
			IgnoreExtraArgs = true,
			DefaultRunMode = RunMode.Async,
		});

		builder.Services.AddRazorPages();
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddCors();

		builder.Services.AddSingleton<ClubberDiscordClient>();
		builder.Services.AddSingleton(commands);
		builder.Services.AddSingleton<MessageHandlerService>();
		builder.Services.AddSingleton<InteractionHandler>();
		builder.Services.AddSingleton<RegistrationTracker>();

		builder.Services.AddTransient<ScoreRoleService>();
		builder.Services.AddTransient<IDiscordHelper, DiscordHelper>();
		builder.Services.AddTransient<IDatabaseHelper, DatabaseHelper>();
		builder.Services.AddTransient<UserService>();
		builder.Services.AddTransient<IWebService, WebService>();

		builder.Services.AddHttpClient();
		builder.Services.AddDbContext<DbService>(ServiceLifetime.Transient);

		if (builder.Environment.IsProduction())
		{
			builder.Services.AddSingleton<UserJoinHandler>();
			builder.Services.AddSingleton<RegistrationRequestHandler>();
			builder.Services.AddHostedService<DdNewsPostService>();
			builder.Services.AddHostedService<DatabaseUpdateService>();
			builder.Services.AddHostedService<KeepAppAliveService>();
			builder.Services.AddHostedService<ChannelClearingService>();
		}

		builder.Services.AddSwaggerGen(options =>
		{
			options.EnableAnnotations();
			options.SwaggerDoc("Main", new OpenApiInfo
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
		app.Services.GetRequiredService<MessageHandlerService>();
		app.Services.GetRequiredService<RegistrationRequestHandler>();
		app.Services.GetRequiredService<InteractionHandler>();

		if (app.Environment.IsProduction())
		{
			app.Services.GetRequiredService<UserJoinHandler>();
		}

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

	private static void ConfigureLogging(this WebApplicationBuilder builder, AppConfig config)
	{
		builder.Logging.ClearProviders();

		Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.MinimumLevel.Information()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u4}] {Message:lj}{NewLine}{Exception}",
				formatProvider: CultureInfo.InvariantCulture)
			.WriteTo.Discord(config.ClubberLoggerId, config.ClubberLoggerToken)
			.CreateLogger();
	}
}
