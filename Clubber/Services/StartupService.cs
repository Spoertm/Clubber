using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Clubber
{
	public class StartupService
	{
		private readonly IServiceProvider _provider;
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private readonly IConfigurationRoot _config;

		// DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
		public StartupService(
			IServiceProvider provider,
			DiscordSocketClient discord,
			CommandService commands,
			IConfigurationRoot config)
		{
			_provider = provider;
			_config = config;
			_discord = discord;
			_commands = commands;
		}

		public async Task StartAsync()
		{
			string discordToken = _config["tokens:discord"];     // Get the discord token from the config file
			if (string.IsNullOrWhiteSpace(discordToken))
				throw new Exception("Please enter your bot's token into the `_config.yml` file found in the applications root directory.");

			await _discord.SetGameAsync("your roles", null, ActivityType.Watching);

			await _discord.LoginAsync(TokenType.Bot, discordToken);     // Login to discord
			await _discord.StartAsync();                                // Connect to the websocket

			_discord.Ready += OnReady;
		}

		public async Task OnReady()
		{
			await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _provider);
		}
	}
}
