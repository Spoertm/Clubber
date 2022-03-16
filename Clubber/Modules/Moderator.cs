using Clubber.Helpers;
using Clubber.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Modules;

[Name("Moderator")]
[RequireRole(697777821954736179, ErrorMessage = "Only moderators can use this command.")]
[RequireContext(ContextType.Guild)]
public class Moderator : ExtendedModulebase<SocketCommandContext>
{
	private readonly IConfiguration _config;
	private readonly IDiscordHelper _discordHelper;

	public Moderator(IConfiguration config, IDiscordHelper discordHelper)
	{
		_config = config;
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

		SocketTextChannel ddnewsPostChannel = _discordHelper.GetTextChannel(_config.GetValue<ulong>("DdNewsChannelId"));
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

		SocketTextChannel ddnewsPostChannel = _discordHelper.GetTextChannel(_config.GetValue<ulong>("DdNewsChannelId"));
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
		ulong registerChannelId = _config.GetValue<ulong>("RegisterChannelId");
		if (Context.Channel is not SocketTextChannel currentTextChannel || currentTextChannel.Id != registerChannelId)
		{
			await ReplyAsync($"This command can only be run in <#{registerChannelId}>.");
			return;
		}

		IEnumerable<IMessage> lastHundredMessages = await currentTextChannel.GetMessagesAsync().FlattenAsync();
		IEnumerable<IMessage> messagesToDelete = lastHundredMessages
			.OrderByDescending(m => m.CreatedAt)
			.SkipLast(1);

		await currentTextChannel.DeleteMessagesAsync(messagesToDelete);
	}
}
