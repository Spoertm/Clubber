using Clubber;
using Clubber.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ClubberDatabaseUpdateCron
{
	public static class Program
	{
		private const ulong _modsChannelId = 701124439990993036;
		private static DiscordSocketClient _client = null!;

		public static void Main() => RunAsync().GetAwaiter().GetResult();

		public static async Task RunAsync()
		{
			_client = new DiscordSocketClient(new DiscordSocketConfig() { AlwaysDownloadUsers = true });

			await _client.LoginAsync(TokenType.Bot, Constants.Token);
			await _client.StartAsync();
			_client.Ready += OnReady;

			await Task.Delay(-1);
		}

		public static async Task OnReady()
		{
			List<Exception> exceptionList = new();
			await Clubber.Program.GetDatabaseFileIntoFolder((_client.GetChannel(Constants.DatabaseBackupChannel) as SocketTextChannel)!, (_client.GetChannel(Constants.ClubberExceptionsChannel) as SocketTextChannel)!);

			SocketGuild? ddPals = _client.GetGuild(Constants.DdPals);
			IMessageChannel? modsChannel = _client.GetChannel(_modsChannelId) as IMessageChannel;

			IUserMessage msg = await modsChannel!.SendMessageAsync("Processing...");
			Stopwatch stopwatch = new();

			int tries = 0;
			const int maxTries = 5;

			bool success = false;
			do
			{
				try
				{
					stopwatch.Restart();

					DatabaseUpdateResponse response = await UpdateRolesHelper.UpdateRolesAndDb(ddPals.Users);
					long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

					if (response.NonMemberCount > 0)
						await modsChannel!.SendMessageAsync($"ℹ️ Unable to update {response.NonMemberCount} user(s) because they're not in the server.");

					int updatedUsers = 0;
					foreach (UpdateRolesResponse updateResponse in response.UpdateResponses.Where(ur => ur.Success))
					{
						await modsChannel!.SendMessageAsync(null, false, EmbedHelper.UpdateRoles(updateResponse));
						updatedUsers++;
					}

					if (updatedUsers > 0)
						await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and {updatedUsers} user(s).\n🕐 Execution took {elapsedMilliseconds} ms.");
					else
						await msg.ModifyAsync(m => m.Content = $"No updates needed today.\nExecution took {elapsedMilliseconds} ms.");

					success = true;
				}
				catch (Exception ex)
				{
					exceptionList.Add(ex);
					tries++;
					if (tries > maxTries)
					{
						await modsChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");

						SocketTextChannel? clubberExceptionsChannel = _client.GetChannel(Constants.ClubberExceptionsChannel) as SocketTextChannel;
						foreach (Exception exc in exceptionList.GroupBy(e => e.ToString()).Select(group => group.First()))
							await clubberExceptionsChannel!.SendMessageAsync(null, false, EmbedHelper.Exception(exc));

						break;
					}
					else
					{
						await modsChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
						System.Threading.Thread.Sleep(10000); // Sleep 10s
					}
				}
			}
			while (!success);

			Environment.Exit(0);
		}
	}
}
