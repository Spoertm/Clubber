using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Clubber.Modules
{
	[Name("Owner")]
	[RequireOwner]
	public class OwnerModule : ModuleBase<SocketCommandContext>
	{
		[Command("changebotname")]
		[Summary("Changes the bot's username.")]
		public async Task ChangeBotName(string username)
		{
			if (username != null)
			{
				await ReplyAsync("Bot username should change in a moment.");
				await Context.Client.CurrentUser.ModifyAsync(x => x.Username = username);
			}
		}

		[Command("changebotavatar")]
		[Summary("Changes the bot's avatar. Specify either the URL of the image or attach it.")]
		public async Task ChangeBotAvatar(string imgUrl = null, string image = null)
		{
			HttpClient Client = new HttpClient();
			Stream stream = new MemoryStream();

			if (string.IsNullOrWhiteSpace(imgUrl) && Context.Message.Attachments.Count == 0)
			{ await ReplyAsync("Invalid arguments."); return; }

			else if (Context.Message.Attachments.Count == 0)
			{
				if (!(Uri.TryCreate(imgUrl, UriKind.Absolute, out Uri uriResult) &&
					 (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)))
				{ await ReplyAsync("Invalid URL."); return; }

				stream = await Client.GetStreamAsync(imgUrl);
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