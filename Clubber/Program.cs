using Clubber.BackgroundTasks;
using Clubber.Helpers;
using Clubber.Models;
using Clubber.Models.Logging;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Globalization;
using System.Reflection;

namespace Clubber;

public static class Program
{
	private static readonly CancellationTokenSource _source = new();

	private static async Task Main()
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		AppDomain.CurrentDomain.ProcessExit += StopBot;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		if (builder.Environment.IsProduction())
			SetConfigFromDb(builder);

		ConfigureLogging(builder.Configuration);
		Log.Information("Starting");

		DiscordSocketClient client = new(new()
		{
			LogLevel = LogSeverity.Error,
			AlwaysDownloadUsers = true,
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
		});

		CommandService commands = new(new()
		{
			IgnoreExtraArgs = true,
			CaseSensitiveCommands = false,
			DefaultRunMode = RunMode.Async,
		});

		client.Log += OnLog;
		commands.Log += OnLog;

		WebApplication app = ConfigureServices(builder, client, commands).Build();

		app.UseSwagger();
		app.UseSwaggerUI();

		RegisterEndpoints(app);

		app.Services.GetRequiredService<MessageHandlerService>();
		app.Services.GetRequiredService<IDatabaseHelper>();
		app.Services.GetRequiredService<WelcomeMessage>();

		app.UseHttpsRedirection();
		app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin());

		await client.LoginAsync(TokenType.Bot, app.Configuration["BotToken"]);
		await client.StartAsync();
		await client.SetGameAsync("your roles", null, ActivityType.Watching);
		await commands.AddModulesAsync(Assembly.GetEntryAssembly(), app.Services);

		client.Ready += async () =>
		{
			try
			{
				await app.RunAsync(_source.Token);
			}
			catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
			{
				Log.Warning("Program cancellation requested");
			}
			finally
			{
				Log.Information("Exiting");
				await client.LogoutAsync();
				await client.DisposeAsync();
				_source.Dispose();
				AppDomain.CurrentDomain.ProcessExit -= StopBot;
			}
		};

		await Task.Delay(-1, _source.Token);
	}

	private static void ConfigureLogging(IConfiguration config) =>
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u4}] {Message:lj}{NewLine}{Exception}")
			.WriteTo.Discord(config.GetValue<ulong>("ClubberLoggerId"), config["ClubberLoggerToken"])
			.CreateLogger();

	private static void RegisterEndpoints(WebApplication app)
	{
		app.MapGet("/", async context =>
		{
			string indexHtmlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Pages", "Index.html");
			string indexHtml = await File.ReadAllTextAsync(indexHtmlPath);
			await context.Response.WriteAsync(indexHtml);
		});

		app.MapGet("/users", async (IDatabaseHelper dbhelper) => await dbhelper.GetEntireDatabase());

		app.MapGet("/users/by-leaderboardId", (int leaderboardId, IServiceScopeFactory scopeFactory) =>
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			return dbContext.DdPlayers.AsNoTracking().FirstOrDefault(user => user.LeaderboardId == leaderboardId);
		});

		app.MapGet("/users/by-discordId", (ulong discordId, IServiceScopeFactory scopeFactory) =>
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			return dbContext.DdPlayers.AsNoTracking().FirstOrDefault(user => user.DiscordId == discordId);
		});

		app.MapGet("/dailynews", async (IServiceScopeFactory scopeFactory) =>
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			await using DbService dbContext = scope.ServiceProvider.GetRequiredService<DbService>();
			return await dbContext.DdNews.AsNoTracking().ToListAsync();
		});
	}

	private static void SetConfigFromDb(WebApplicationBuilder builder)
	{
		using DbService dbService = new();
		string jsonConfig = dbService.ClubberConfig.AsNoTracking().First().JsonConfig;
		string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DbConfig.json");
		File.WriteAllText(configPath, jsonConfig);
		builder.Configuration.AddJsonFile(configPath);
	}

	private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder, DiscordSocketClient client, CommandService commands)
	{
		builder.Logging.ClearProviders();
		builder.Services
			.AddEndpointsApiExplorer()
			.AddSwaggerGen()
			.AddCors()
			.AddSingleton(client)
			.AddSingleton(commands)
			.AddSingleton<MessageHandlerService>()
			.AddSingleton<IDatabaseHelper, DatabaseHelper>()
			.AddSingleton<UpdateRolesHelper>()
			.AddSingleton<IDiscordHelper, DiscordHelper>()
			.AddSingleton<UserService>()
			.AddSingleton<IWebService, WebService>()
			.AddSingleton<WelcomeMessage>()
			.AddHostedService<DdNewsPostService>()
			.AddHostedService<DatabaseUpdateService>()
			.AddHttpClient()
			.AddDbContext<DbService>();

		return builder;
	}

	private static async Task OnLog(LogMessage logMessage)
	{
		if (logMessage.Exception is CommandException commandException)
		{
			await commandException.Context.Channel.SendMessageAsync("Catastrophic error occured.");
			if (commandException.InnerException is CustomException customException)
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

		string message = $"Source: {logMessage.Source}\n{logMessage.Message}";
		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		Log.Logger.Write(logLevel, logMessage.Exception, message);
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		=> Log.Fatal(e.ExceptionObject as Exception, "Caught unhandled exception. IsTerminating: {}", e.IsTerminating);

	private static void StopBot(object? sender, EventArgs e) => StopBot();

	public static void StopBot() => _source.Cancel();
}
