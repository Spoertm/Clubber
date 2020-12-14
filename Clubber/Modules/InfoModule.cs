using System.Threading.Tasks;
using Discord.Commands;

namespace Clubber.Modules
{
	[Name("Info")]
	public class InfoModule : ModuleBase<SocketCommandContext>
	{
		[Command("whyareyou")]
		[Summary("Describes what the bot does.")]
		public async Task WhyAreYou()
			=> await ReplyAsync("Every day or so, I automatically update people's DD roles.\nFor example, if someone beats their score of 300s and gets 400s, I update their role from `300+ club` to `400+ club`.\nTo speed this up, you can manually update your own or someone else's roles by using the `updateroles` command.");
	}
}