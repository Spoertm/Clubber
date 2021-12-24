using Clubber.Helpers;
using Clubber.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.Modules
{
	[Name("Moderator")]
	[Summary("Obtains user from their leaderboard ID and adds them to the database.")]
	[RequireRole(697777821954736179, ErrorMessage = "Only moderators can use this command.")]
	[RequireContext(ContextType.Guild)]
	public class Moderator : ExtendedModulebase<SocketCommandContext>
	{
		private readonly IDiscordHelper _discordHelper;

		public Moderator(IDiscordHelper discordHelper) => _discordHelper = discordHelper;

		[Command("editnewspost")]
		[Summary("Allows you to edit a DD news post made by the bot. If no message ID is given, then the latest post will be edited.")]
		[Remarks("editnewspost 123456789 This is the new text!")]
		[Priority(1)]
		public async Task EditDdNewsPost([Name("message ID")] ulong messageId, [Name("text")][Remainder] string newMessage)
		{
			if (await IsError(string.IsNullOrWhiteSpace(newMessage), "Message can't be empty."))
				return;

			SocketTextChannel ddnewsPostChannel = _discordHelper.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("DdNewsChannelId")!));
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

			SocketTextChannel ddnewsPostChannel = _discordHelper.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("DdNewsChannelId")!));
			IEnumerable<IMessage> messages = await ddnewsPostChannel.GetMessagesAsync(5).FlattenAsync();
			IUserMessage? messageToEdit = messages.Where(m => m.Author.Id == Context.Client.CurrentUser.Id)
				.OrderBy(m => m.CreatedAt.Date)
				.FirstOrDefault() as IUserMessage;

			if (messageToEdit is null)
			{
				await InlineReplyAsync("Could not find message.");
				return;
			}

			await messageToEdit.ModifyAsync(m => m.Content = newMessage);
			await ReplyAsync("✅ Done!");
		}
	}
}
