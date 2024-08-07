﻿using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Clubber.Discord.Modules;

[Name("Moderator")]
[RequireAdminOrRole(697777821954736179, ErrorMessage = "Only moderators can use this command.")]
[RequireContext(ContextType.Guild)]
public class Moderator : ExtendedModulebase<SocketCommandContext>
{
	private readonly AppConfig _config;
	private readonly IDiscordHelper _discordHelper;

	public Moderator(IOptions<AppConfig> config, IDiscordHelper discordHelper)
	{
		_config = config.Value;
		_discordHelper = discordHelper;
	}

	[Command("editnewspost")]
	[Summary("Allows you to edit a DD news post made by the bot. If no message ID is given, then the latest post will be edited.")]
	[Remarks("editnewspost 123456789 This is the new text!")]
	[Priority(1)]
	public async Task EditDdNewsPost([Name("message ID")] ulong messageId, [Name("text")][Remainder] string newMessage)
	{
		if (await IsError(string.IsNullOrWhiteSpace(newMessage), "Message can't be empty."))
			return;

		SocketTextChannel ddnewsPostChannel = _discordHelper.GetTextChannel(_config.DdNewsChannelId);
		if (await ddnewsPostChannel.GetMessageAsync(messageId) is not IUserMessage messageToEdit)
		{
			await InlineReplyAsync("Could not find message.");
			return;
		}

		if (await IsError(messageToEdit.Author.Id != Context.Client.CurrentUser.Id, "That message wasn't posted by the bot."))
			return;

		await messageToEdit.ModifyAsync(m => m.Content = newMessage);
		await ReplyAsync("✅ Done!");
	}

	[Command("editnewspost")]
	[Summary("Allows you to edit a DD news post made by the bot. If no message ID is given, then the latest post will be edited.")]
	[Remarks("editnewspost This is the new text!")]
	[Priority(0)]
	public async Task EditDdNewsPost([Name("text")][Remainder] string newMessage)
	{
		if (await IsError(string.IsNullOrWhiteSpace(newMessage), "Message can't be empty."))
			return;

		SocketTextChannel ddnewsPostChannel = _discordHelper.GetTextChannel(_config.DdNewsChannelId);
		IEnumerable<IMessage> messages = await ddnewsPostChannel.GetMessagesAsync(5).FlattenAsync();
		if (messages.Where(m => m.Author.Id == Context.Client.CurrentUser.Id).MaxBy(m => m.CreatedAt) is not IUserMessage messageToEdit)
		{
			await InlineReplyAsync("Could not find message.");
			return;
		}

		await messageToEdit.ModifyAsync(m => m.Content = newMessage);
		await ReplyAsync("✅ Done!");
	}

	[Command("clear")]
	[Summary("Clears all messages but the first one in #register channel.")]
	[Remarks("clear")]
	public async Task Clear()
	{
		ulong registerChannelId = _config.RegisterChannelId;
		if (Context.Channel is not SocketTextChannel channel || channel.Id != registerChannelId)
		{
			await ReplyAsync($"This command can only be run in <#{registerChannelId}>.");
			return;
		}

		await _discordHelper.ClearChannelAsync(channel);
		await channel.SendMessageAsync(embeds: EmbedHelper.RegisterEmbeds());
	}
}
