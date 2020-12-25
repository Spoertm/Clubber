﻿using Discord;
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
		private readonly IServiceProvider provider;
		private readonly DiscordSocketClient discord;
		private readonly CommandService commands;
		private readonly IConfigurationRoot config;

		// DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
		public StartupService(
			IServiceProvider _provider,
			DiscordSocketClient _discord,
			CommandService _commands,
			IConfigurationRoot _config)
		{
			provider = _provider;
			config = _config;
			discord = _discord;
			commands = _commands;
		}

		public async Task StartAsync()
		{
			string discordToken = config["tokens:discord"];     // Get the discord token from the config file
			if (string.IsNullOrWhiteSpace(discordToken))
				throw new Exception("Please enter your bot's token into the `_config.yml` file found in the applications root directory.");

			await discord.SetGameAsync("your roles", null, ActivityType.Watching);

			await discord.LoginAsync(TokenType.Bot, discordToken);     // Login to discord
			await discord.StartAsync();                                // Connect to the websocket

			await commands.AddModulesAsync(Assembly.GetExecutingAssembly(), provider);     // Load commands and modules into the command service
		}
	}
}