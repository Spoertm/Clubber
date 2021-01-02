using Clubber;
using Clubber.Databases;
using Clubber.Helpers;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Clubber.Helpers.UpdateRolesHelper;

namespace ClubberDatabaseUpdateCron
{
	public class Startup
	{
		private DiscordSocketClient _client;
		private ServiceProvider _provider;
		private SocketTextChannel _testingChannel;

		public async Task RunAsync()
		{
			ServiceCollection services = new();
			ConfigureServices(services);

			_provider = services.BuildServiceProvider();
			_client = _provider.GetRequiredService<DiscordSocketClient>();

			const string discordToken = "NzQzNDMxNTAyODQyMjk4MzY4.XzUkig.UQrKlF7axeeFqewonpkTTAwaIIo";
			await _client.LoginAsync(TokenType.Bot, discordToken);
			await _client.StartAsync();

			_client.Ready += OnReady;

			await Task.Delay(-1);
		}

		public async Task OnReady()
		{
			SocketGuild ddPals = _client.GetGuild(399568958669455364);
			_testingChannel = ddPals.GetTextChannel(447487662891466752);
			Stopwatch stopwatch = new Stopwatch();

			int tries = 1;
			const int maxTries = 5;
			await _testingChannel.SendMessageAsync("🗡 Attempting database update...");

			UpdateRolesHelper updateRoleHelper = _provider.GetRequiredService<UpdateRolesHelper>();

			bool success = false;
			do
			{
				try
				{
					stopwatch.Restart();

					UpdateRolesResponse response = await updateRoleHelper.UpdateRolesAndDb();
					success = true;

					if (response.NonMemberCount > 0)
						await _testingChannel.SendMessageAsync($"ℹ️ Unable to update {response.NonMemberCount} user(s). They're most likely not in the server.");

					foreach (UpdateRoleResponse updateResponse in response.UpdateResponses.Where(u => u.Success))
					{
						await WriteRoleUpdateEmbed(updateResponse);
					}

					if (response.UpdatedUsers > 0)
						await _testingChannel.SendMessageAsync($"✅ Successfully updated database and member roles for {response.UpdatedUsers} user(s).\nExecution took {stopwatch.ElapsedMilliseconds} ms");
					else
						await _testingChannel.SendMessageAsync($"No role updates were needed.\nExecution took {stopwatch.ElapsedMilliseconds} ms");
				}
				catch
				{
					tries++;
					if (tries > maxTries)
					{
						await _testingChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");
						break;
					}
					else
					{
						await _testingChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
						System.Threading.Thread.Sleep(10000); // Sleep 10s
					}
				}
			}
			while (!success);

			Environment.Exit(0);
		}

		public async Task WriteRoleUpdateEmbed(UpdateRoleResponse response)
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle($"Updated roles for {response.GuildMember.Username}")
				.WithDescription($"User: {response.GuildMember.Mention}")
				.WithThumbnailUrl(response.GuildMember.GetAvatarUrl() ?? response.GuildMember.GetDefaultAvatarUrl());

			if (response.RemovedRoles.Count > 0)
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Removed:")
					.WithValue(string.Join('\n', response.RemovedRoles.Select(rr => rr.Mention)))
					.WithIsInline(true));
			}

			if (response.AddedRoles.Count > 0)
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Added:")
					.WithValue(string.Join('\n', response.AddedRoles.Select(ar => ar.Mention)))
					.WithIsInline(true));
			}

			await _testingChannel.SendMessageAsync(null, false, embed.Build());
		}

		private static void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Error,
				MessageCacheSize = 1000,
				AlwaysDownloadUsers = true,
			}))
			.AddSingleton(new CommandService(new CommandServiceConfig
			{
				LogLevel = LogSeverity.Error,
				DefaultRunMode = RunMode.Async,
				CaseSensitiveCommands = false,
				IgnoreExtraArgs = true,
			}))
			.AddSingleton<InteractiveService>()
			.AddSingleton<CommandHandler>()
			.AddSingleton<StartupService>()
			.AddSingleton<LoggingService>()
			.AddSingleton<MongoDatabase>()
			.AddSingleton<ScoreRoles>()
			.AddSingleton<UpdateRolesHelper>()
			.AddSingleton<DatabaseHelper>();
		}
	}
}
