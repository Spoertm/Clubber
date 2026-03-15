using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Modules;
using Clubber.Discord.Services;
using Clubber.Domain.BackgroundTasks;
using Clubber.Domain.Helpers;
using Clubber.Domain.Repositories;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Clubber.Web.Configuration;
using Clubber.Web.Endpoints;
using Discord.Commands;
using Npgsql;
using Serilog;
using System.Globalization;
using Scalar.AspNetCore;

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

		Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
		builder.Host.UseSerilog();

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
		builder.Services.AddSingleton<CommandService>(_ =>
		{
			CommandService commands = new(new CommandServiceConfig
			{
				IgnoreExtraArgs = true,
				DefaultRunMode = RunMode.Async,
			});

			return commands;
		});
		builder.Services.AddTransient<ComponentInteractions>();
		builder.Services.AddSingleton<TextCommandHandler>();
		builder.Services.AddSingleton<RegistrationTracker>();

		builder.Services.AddTransient<ScoreRoleService>();
		builder.Services.AddTransient<IDiscordHelper, DiscordHelper>();
		builder.Services.AddTransient<IUserRepository, UserRepository>();
		builder.Services.AddTransient<INewsRepository, NewsRepository>();
		builder.Services.AddTransient<ILeaderboardRepository, LeaderboardRepository>();
		builder.Services.AddTransient<UserService>();
		builder.Services.AddTransient<IWebService, WebService>();

		builder.Services.AddHttpClient();
		builder.Services.AddDbContext<DbService>(ServiceLifetime.Scoped);

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

		// Scalar OpenAPI documentation
		builder.Services.AddOpenApi(options =>
		{
			options.AddDocumentTransformer((document, context, cancellationToken) =>
			{
				document.Info = new()
				{
					Title = "Clubber API",
					Version = "v1",
					Description = """
					              RESTful API for the Clubber Discord bot. Access Devil Daggers community data including
					              registered users, daily news, best splits, and more. Perfect for building integrations
					              and custom applications for the DD community.
					              """,
					Contact = new()
					{
						Name = "Spoertm",
						Url = new Uri("https://github.com/Spoertm"),
					},
					License = new()
					{
						Name = "View on GitHub",
						Url = new Uri("https://github.com/Spoertm/Clubber"),
					},
				};

				return Task.CompletedTask;
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

		// Scalar API documentation
		app.MapOpenApi();
		app.MapScalarApiReference(options =>
		{
			options.Title = "Clubber API Documentation";
			options.Theme = ScalarTheme.Mars;
		});

		// Map MVC routes FIRST
		app.MapControllerRoute(
			"default",
			"{controller=Home}/{action=Index}/{id?}");

		app.RegisterClubberEndpoints();

		try
		{
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
}
