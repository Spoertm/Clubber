using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Clubber.Modules
{
    [Name("General Commands")]
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        public readonly CommandService service;
        public readonly IConfigurationRoot config;
        private static readonly HttpClient Client = new HttpClient();

        public GeneralCommands(CommandService _service, IConfigurationRoot _config)
        {
            service = _service;
            config = _config;
        }

        [Command("help")]
        [Summary("Shows info about command, otherwise command list.")]
        public async Task HelpAsync([Remainder] string command = null)
        {
            if (!string.IsNullOrWhiteSpace(command))    // If command exists
            {
                var result = service.Search(Context, command);
                var preCondCheck = result.IsSuccess ? await result.Commands[0].Command.CheckPreconditionsAsync(Context) : null;

                if (!result.IsSuccess)
                { await ReplyAsync($"The command `{command}` doesn't exist."); return; }
                else if (!preCondCheck.IsSuccess)
                { await ReplyAsync($"The command `{command}` couldn't be executed.\nReason: " + preCondCheck.ErrorReason); return; }

                string prefix = config["prefix"], aliases = "";
                var builder = new EmbedBuilder();

                var cmd = result.Commands[0].Command;
                if (cmd.Aliases.Count > 1) aliases = $"\nAliases: {string.Join(", ", cmd.Aliases)}";

                builder.Title = $"{cmd.Name} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**[{p.Name}]**" : $"**[{p.Name} = {p.DefaultValue}]**" : $"**<{p.Name}>**"))}{aliases}";

                builder.Description = cmd.Summary;

                if (cmd.Parameters.Count > 0)
                    builder.Footer = new EmbedFooterBuilder() { Text = "<>: Required⠀⠀[]: Optional\nText within \" \" will be counted as one argument." };

                await ReplyAsync("", false, builder.Build());
            }
            else    // If command doesnt exist
            {
                string prefix = config["prefix"];
                EmbedBuilder builder = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "List of available commands",
                        IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                    }
                };

                foreach (ModuleInfo module in service.Modules)
                {
                    string description = null;
                    foreach (var cmd in module.Commands)
                    {
                        PreconditionResult result = await cmd.CheckPreconditionsAsync(Context);
                        if (result.IsSuccess)
                            description += $"{cmd.Remarks}{(cmd.Remarks == null ? prefix : "")}{string.Join("/", cmd.Aliases)}\n";
                    }

                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        builder.AddField(x =>
                        {
                            x.Name = $"{module.Name}";
                            x.Value = description;
                            x.IsInline = true;
                        });
                    }
                }
                builder.AddField("\u200B", $"Mentioning the bot works as well as using the prefix.\nUse `{prefix}help [command]` or call a command to get more info.");
                await ReplyAsync("", false, builder.Build());
            }
        }

        [Command("whyareyou")]
        [Summary("Describes what the bot does.")]
        public async Task WhyAreYou()
            => await ReplyAsync("I periodically update people's score/club roles. Most of my commands are admin-only, which means you can't see/use them if you're not an admin.");

        [Command("changebotname")]
        [Summary("Changes the bot's username.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ChangeBotName(string username)
        {
            if (username != null)
            {
                await ReplyAsync("Bot username should change in a moment.");
                await Context.Client.CurrentUser.ModifyAsync(x => x.Username = username);
            }
        }

        [Command("changebotavatar")]
        [Summary("Changes the bot's avatar.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ChangeBotAvatar(string avatarURL = null, string image = null)
        {
            Stream stream = new MemoryStream();
            if (string.IsNullOrWhiteSpace(avatarURL) && Context.Message.Attachments.Count == 0)
            { await ReplyAsync("Invalid arguments."); return; }

            else if (Context.Message.Attachments.Count == 0)
            {
                if (!(Uri.TryCreate(avatarURL, UriKind.Absolute, out Uri uriResult) &&
                     (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)))
                { await ReplyAsync("Invalid URL."); return; }

                stream = await Client.GetStreamAsync(avatarURL);
            }
            else if (Context.Message.Attachments.Count > 0)
            {
                string[] imageFormats = new string[] { "jpg", "jpeg", "png", "gif" };
                var atchm = Context.Message.Attachments.First();

                if (!imageFormats.Contains(atchm.Filename.Substring(atchm.Filename.Length - 3)))
                { await ReplyAsync("Image file format has to be of type: " + string.Join('/', imageFormats)); return; }

                stream = await Client.GetStreamAsync(Context.Message.Attachments.First().Url);
            }
            await ReplyAsync("Bot avatar should change in a moment.");
            await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(stream));
        }
    }
}
