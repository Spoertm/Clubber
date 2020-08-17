using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

namespace Clubber
{
    public class StartupService
    {
        private readonly IServiceProvider provider;
        private readonly DiscordSocketClient discord;
        private readonly CommandService commands;
        private readonly IConfigurationRoot config;
        private ISocketMessageChannel Channel;
        private SocketUserMessage msg;
        private System.Threading.Timer Timer;
        EmbedBuilder embed = new EmbedBuilder { Title = "Periodic database update" };

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

            discord.Ready += ReadyToUpdateDb;
        }

        private async Task ReadyToUpdateDb()
        {
            Channel = discord.Guilds.FirstOrDefault(g => g.Id == 399568958669455364).TextChannels.FirstOrDefault(tch => tch.Id == 447487662891466752);
            if (Channel != null)
            {
                try
                {
                    var restMsg = await Channel.SendMessageAsync("❗");
                    msg = await restMsg.Channel.GetMessageAsync(restMsg.Id) as SocketUserMessage;
                    Timer = new System.Threading.Timer(UpdateDb, null, 0, 1000 * 60 * 60 * 24);
                }
                catch { await msg.Channel.SendMessageAsync("Failed to execute daily database update."); }
            }
        }

        private void UpdateDb(object state)
        {
            try
            {
                var context = new SocketCommandContext(discord, msg);
                embed.Description = $"Current time: **{DateTime.Now}**\n\nNext run should be on **{DateTime.Now.AddDays(1)}**";
                context.Channel.SendMessageAsync(null, false, embed.Build());
                commands.ExecuteAsync(context, "updatedb", provider);
            }
            catch { msg.Channel.SendMessageAsync("Failed to execute daily database update."); }
        }
    }
}
