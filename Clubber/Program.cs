using Clubber.BackgroundTasks;
using Clubber.Helpers;
using Clubber.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Globalization;
using System.Reflection;

namespace Clubber
{
	public static class Program
	{
		private static readonly CancellationTokenSource _source = new();

		private static async Task Main()
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			AppDomain.CurrentDomain.ProcessExit += StopBot;

			DiscordSocketClient client = new(new() { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true, LogLevel = LogSeverity.Error });
			CommandService commands = new(new() { IgnoreExtraArgs = true, CaseSensitiveCommands = false, DefaultRunMode = RunMode.Async });

			WebApplication app = ConfigureServices(client, commands).Build();

			app.UseSwagger();
			app.UseSwaggerUI();

			RegisterEndpoints(app);

			await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("BotToken"));
			await client.StartAsync();
			await client.SetGameAsync("your roles", null, ActivityType.Watching);
			await commands.AddModulesAsync(Assembly.GetEntryAssembly(), app.Services);

			app.Services.GetRequiredService<MessageHandlerService>();
			app.Services.GetRequiredService<IDatabaseHelper>();
			app.Services.GetRequiredService<WelcomeMessage>();
			app.Services.GetRequiredService<LoggingService>();

			app.UseHttpsRedirection();
			app.UseAuthorization();
			app.MapControllers();

			try
			{
				await app.RunAsync(_source.Token);
			}
			finally
			{
				await client.LogoutAsync();
				client.Dispose();
				_source.Dispose();
				AppDomain.CurrentDomain.ProcessExit -= StopBot;
			}
		}

		private static void RegisterEndpoints(WebApplication app)
		{
			app.MapGet("/", async context =>
			{
				string indexHtmlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Pages", "Index.html");
				string indexHtml = await File.ReadAllTextAsync(indexHtmlPath);
				await context.Response.WriteAsync(indexHtml);
			});

			app.MapGet("/users", (IDatabaseHelper databaseHelper)
				=> databaseHelper.DdUserDatabase);

			app.MapGet("/users/by-leaderboardId", (int leaderboardId, IDatabaseHelper databaseHelper)
				=> databaseHelper.DdUserDatabase.Find(user => user.LeaderboardId == leaderboardId));

			app.MapGet("/users/by-discordId", (ulong discordId, IDatabaseHelper databaseHelper)
				=> databaseHelper.DdUserDatabase.Find(user => user.DiscordId == discordId));
		}

		private static WebApplicationBuilder ConfigureServices(DiscordSocketClient client, CommandService commands)
		{
			WebApplicationBuilder builder = WebApplication.CreateBuilder();
			builder.Logging.ClearProviders();
			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.AddSingleton(client)
				.AddSingleton(commands)
				.AddSingleton<MessageHandlerService>()
				.AddSingleton<IDatabaseHelper, DatabaseHelper>()
				.AddSingleton<UpdateRolesHelper>()
				.AddSingleton<IDiscordHelper, DiscordHelper>()
				.AddSingleton<UserService>()
				.AddSingleton<IWebService, WebService>()
				.AddSingleton<LoggingService>()
				.AddSingleton<WelcomeMessage>()
				.AddSingleton<ImageGenerator>()
				.AddHostedService<DdNewsPostService>()
				.AddHostedService<DatabaseUpdateService>()
				.AddHttpClient()
				.AddDbContext<DatabaseService>();

			return builder;
		}

		private static void StopBot(object? sender, EventArgs e) => StopBot();

		public static void StopBot() => _source.Cancel();
	}
}
