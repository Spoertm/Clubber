using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Logging;
using Clubber.Domain.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Globalization;

namespace Clubber.Web.Server;

internal static class Program
{
	public static async Task Main(string[] args)
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		if (builder.Environment.IsProduction())
		{
			SetConfigFromDb(builder);
		}

		ConfigureLogging(builder.Configuration);
		Log.Information("Starting");

		builder.Services.AddRazorPages();
		builder.Services.AddServerSideBlazor();
		builder.Services.AddEndpointsApiExplorer();

		const GatewayIntents gatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent) &
											~GatewayIntents.GuildInvites &
											~GatewayIntents.GuildScheduledEvents;

		DiscordSocketClient client = new(new()
		{
			LogLevel = LogSeverity.Warning,
			AlwaysDownloadUsers = true,
			GatewayIntents = gatewayIntents,
		});

		CommandService commands = new(new()
		{
			IgnoreExtraArgs = true,
			CaseSensitiveCommands = false,
			DefaultRunMode = RunMode.Async,
		});

		client.Log += OnLog;
		commands.Log += OnLog;

		builder.Logging.ClearProviders();

		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();
		builder.Services.AddCors();
		builder.Services.AddSingleton(client);
		builder.Services.AddSingleton(commands);
		builder.Services.AddSingleton<MessageHandlerService>();
		builder.Services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
		builder.Services.AddSingleton<UpdateRolesHelper>();
		builder.Services.AddSingleton<IDiscordHelper, DiscordHelper>();
		builder.Services.AddSingleton<UserService>();
		builder.Services.AddSingleton<IWebService, WebService>();
		builder.Services.AddHttpClient();
		builder.Services.AddDbContext<DbService>();

		if (builder.Environment.IsProduction())
		{
			builder.Services.AddSingleton<WelcomeMessage>();
			builder.Services.AddHostedService<DdNewsPostService>();
			builder.Services.AddHostedService<DatabaseUpdateService>();
			builder.Services.AddHostedService<KeepAppAliveService>();
		}

		WebApplication app = builder.Build();

		RegisterEndpoints(app);

		app.UseSwagger();

		app.UseSwaggerUI();

		app.Services.GetRequiredService<MessageHandlerService>();
		app.Services.GetRequiredService<IDatabaseHelper>();
		app.Services.GetRequiredService<WelcomeMessage>();

		app.UseHttpsRedirection();

		app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin());

		app.UseStaticFiles();

		app.UseRouting();

		app.MapBlazorHub();

		app.MapRazorPages();

		app.MapFallbackToPage("/_Host");

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
			Log.CloseAndFlush();
		}
	}

	private static void SetConfigFromDb(WebApplicationBuilder builder)
	{
		using DbService dbService = new();
		string jsonConfig = dbService.ClubberConfig.AsNoTracking().First().JsonConfig;
		string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DbConfig.json");
		File.WriteAllText(configPath, jsonConfig);
		builder.Configuration.AddJsonFile(configPath);
	}

	private static void ConfigureLogging(IConfiguration config)
	{
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u4}] {Message:lj}{NewLine}{Exception}")
			.WriteTo.Discord(config.GetValue<ulong>("ClubberLoggerId"), config["ClubberLoggerToken"] ?? throw new ConfigurationMissingException("\"ClubberLoggerToken\""))
			.CreateLogger();
	}

	private static async Task OnLog(LogMessage logMessage)
	{
		if (logMessage.Exception is CommandException commandException)
		{
			await commandException.Context.Channel.SendMessageAsync("Catastrophic error occured.");
			if (commandException.InnerException is ClubberException customException)
				await commandException.Context.Channel.SendMessageAsync(customException.Message);
		}

		LogEventLevel logLevel = logMessage.Severity switch
		{
			LogSeverity.Critical => LogEventLevel.Fatal,
			LogSeverity.Error    => LogEventLevel.Error,
			LogSeverity.Warning  => LogEventLevel.Warning,
			LogSeverity.Info     => LogEventLevel.Information,
			LogSeverity.Verbose  => LogEventLevel.Verbose,
			LogSeverity.Debug    => LogEventLevel.Debug,
			_                    => throw new ArgumentOutOfRangeException(nameof(logMessage.Severity), logMessage.Severity, null),
		};

		Log.Logger.Write(logLevel, logMessage.Exception, "Source: {LogMsgSrc}\n{Msg}", logMessage.Source, logMessage.Message);
	}

	private static void RegisterEndpoints(WebApplication app)
	{
		app.MapGet("/", async context =>
		{
			string indexHtmlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Pages", "Index.html");
			string indexHtml = await File.ReadAllTextAsync(indexHtmlPath);
			await context.Response.WriteAsync(indexHtml);
		});

		app.MapGet("/users", async (IDatabaseHelper dbhelper) => await dbhelper.GetEntireDatabase())
			.WithTags("Users");

		app.MapGet("/users/by-leaderboardId", (int leaderboardId, IServiceScopeFactory scopeFactory) =>
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			return dbContext.DdPlayers.AsNoTracking().FirstOrDefault(user => user.LeaderboardId == leaderboardId);
		}).WithTags("Users");

		app.MapGet("/users/by-discordId", (ulong discordId, IServiceScopeFactory scopeFactory) =>
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			return dbContext.DdPlayers.AsNoTracking().FirstOrDefault(user => user.DiscordId == discordId);
		}).WithTags("Users");

		app.MapGet("/dailynews", async (IServiceScopeFactory scopeFactory) =>
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			return await dbContext.DdNews.AsNoTracking().ToListAsync();
		}).WithTags("News");

		app.MapGet("/bestsplits", async (IServiceScopeFactory scopeFactory) =>
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			return await dbContext.BestSplits.AsNoTracking().ToArrayAsync();
		}).WithTags("Splits");

		app.MapGet("/bestsplits/by-splitname", async (string splitName, IServiceScopeFactory scopeFactory) =>
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			return await dbContext.BestSplits.AsNoTracking().FirstOrDefaultAsync(bs => bs.Name == splitName);
		}).WithTags("Splits");
	}
}
