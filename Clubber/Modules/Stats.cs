using Clubber.Database;
using Clubber.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	[Name("Info")]
	[Group("me")]
	[Alias("stats", "statsf", "statsfull")]
	[Summary("Provides statistics from the leaderboard for users that are in this server and registered.\n`statsf` shows all the information available.")]
	public class Stats : AbstractModule<SocketCommandContext>
	{
		[Command]
		[Remarks("me")]
		[Priority(1)]
		public async Task StatsFromCurrentUser()
			=> await CheckUserAndShowStats(Context.User.Id);

		[Command]
		[Remarks("stats clubber\nstats <@743431502842298368>")]
		[Priority(2)]
		public async Task StatsFromName([Name("name | tag")][Remainder] string name)
		{
			(bool success, SocketGuildUser? user) = await FoundOneUserFromName(name);
			if (success && user != null)
				await CheckUserAndShowStats(user.Id);
		}

		[Command("id")]
		[Remarks("stats id 743431502842298368")]
		[Priority(3)]
		public async Task StatsFromDiscordId([Name("Discord ID")] ulong discordId)
			=> await CheckUserAndShowStats(discordId);

		private async Task CheckUserAndShowStats(ulong discordId)
		{
			SocketGuildUser? user = Context.Guild.GetUser(discordId);
			if (user != null)
			{
				if (!await UserIsClean(user, true, true, false, true))
					return;
			}
			else if (await IsError(!DatabaseHelper.UserIsRegistered(discordId), "User not found."))
			{
				return;
			}

			uint lbPlayerId = (uint)DatabaseHelper.DdUsers.Find(du => du.DiscordId == discordId)!.LeaderboardId;
			LeaderboardUser lbPlayer = DatabaseHelper.GetLbPlayers(new uint[] { lbPlayerId }).Result.First();
			Embed statsEmbed;

			if (Context.Message.Content.StartsWith("+statsf") || Context.Message.Content.StartsWith("+statsfull"))
				statsEmbed = EmbedHelper.GetFullStatsEmbed(lbPlayer, user, discordId);
			else
				statsEmbed = EmbedHelper.GetStatsEmbed(lbPlayer, user, discordId);

			await ReplyAsync(null, false, statsEmbed, null, AllowedMentions.None, new MessageReference(Context.Message.Id));
		}
	}
}
