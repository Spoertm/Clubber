using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Logging;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Modules;
using Clubber.Domain.Services;
using Clubber.Web.Server.Endpoints;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using System.Globalization;
using System.Reflection;

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

		if (builder.Environment.IsProduction())
		{
			SetConfigFromDb(builder);
		}

		ConfigureLogging(builder.Configuration);
		Log.Information("Starting");

		DiscordSocketClient client = new(new()
		{
			LogLevel = LogSeverity.Warning,
			AlwaysDownloadUsers = true,
			GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers) &
							~GatewayIntents.GuildInvites &
							~GatewayIntents.GuildScheduledEvents,
		});

		CommandService commands = new(new()
		{
			IgnoreExtraArgs = true,
			DefaultRunMode = RunMode.Async,
		});

		builder.Logging.ClearProviders();

		builder.Services.AddRazorPages();
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddCors();

		builder.Services.AddSingleton(client);
		builder.Services.AddSingleton(commands);
		builder.Services.AddSingleton<MessageHandlerService>();

		builder.Services.AddTransient<UpdateRolesHelper>();
		builder.Services.AddTransient<IDiscordHelper, DiscordHelper>();
		builder.Services.AddTransient<IDatabaseHelper, DatabaseHelper>();
		builder.Services.AddTransient<UserService>();
		builder.Services.AddTransient<IWebService, WebService>();

		builder.Services.AddHttpClient();
		builder.Services.AddDbContext<DbService>(ServiceLifetime.Transient);

		if (builder.Environment.IsProduction())
		{
			builder.Services.AddSingleton<WelcomeMessage>();
			builder.Services.AddHostedService<DdNewsPostService>();
			builder.Services.AddHostedService<DatabaseUpdateService>();
			builder.Services.AddHostedService<KeepAppAliveService>();
			builder.Services.AddHostedService<ChannelClearingService>();
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

		app.Services.GetRequiredService<MessageHandlerService>();

		if (app.Environment.IsProduction())
		{
			app.Services.GetRequiredService<WelcomeMessage>();
		}

		await client.LoginAsync(TokenType.Bot, app.Configuration["BotToken"]);
		await client.StartAsync();
		await client.SetGameAsync("your roles", null, ActivityType.Watching);
		await commands.AddModulesAsync(Assembly.GetAssembly(typeof(ExtendedModulebase<>)), app.Services);

		// Give the Discord client some time to get ready
		await Task.Delay(TimeSpan.FromSeconds(5));

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
			.MinimumLevel.Information()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u4}] {Message:lj}{NewLine}{Exception}")
			.WriteTo.Discord(config.GetValue<ulong>("ClubberLoggerId"), config["ClubberLoggerToken"] ?? throw new ConfigurationMissingException("ClubberLoggerToken"))
			.CreateLogger();
	}
}
