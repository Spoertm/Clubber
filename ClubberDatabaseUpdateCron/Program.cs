using Clubber;
using Clubber.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ClubberDatabaseUpdateCron
{
	public static class Program
	{
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
			await Clubber.Program.GetDatabaseFileIntoFolder((_client.GetChannel(Constants.DatabaseBackupChannel) as SocketTextChannel)!, (_client.GetChannel(Constants.ClubberExceptionsChannel) as SocketTextChannel)!);

			SocketGuild? ddPals = _client.GetGuild(Constants.DdPals);
			IMessageChannel? testingChannel = _client.GetChannel(Constants.TestingChannel) as IMessageChannel;

			IUserMessage msg = await testingChannel!.SendMessageAsync("Processing...");
			Stopwatch stopwatch = new();

			int tries = 1;
			const int maxTries = 5;

			bool success = false;
			do
			{
				try
				{
					stopwatch.Restart();

					DatabaseUpdateResponse response = await UpdateRolesHelper.UpdateRolesAndDb(ddPals);
					long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

					if (response.NonMemberCount > 0)
						await testingChannel!.SendMessageAsync($"ℹ️ Unable to update {response.NonMemberCount} user(s) because they're not in the server.");

					if (response.UpdatedUsers > 0)
					{
						foreach (UpdateRolesResponse updateResponse in response.UpdateResponses)
						{
							if (updateResponse.Success)
								await testingChannel!.SendMessageAsync(null, false, UpdateRolesHelper.GetUpdateRolesEmbed(updateResponse));
						}

						await msg.ModifyAsync(m => m.Content = $"✅ Successfully updated database and {response.UpdatedUsers} user(s).\n🕐 Execution took {elapsedMilliseconds} ms.");
					}
					else
					{
						await msg.ModifyAsync(m => m.Content = $"No updates needed today.\nExecution took {elapsedMilliseconds} ms.");
					}

					success = true;
				}
				catch
				{
					tries++;
					if (tries > maxTries)
					{
						await testingChannel.SendMessageAsync($"❌ Failed to update DB {maxTries} times then exited.");
						break;
					}
					else
					{
						await testingChannel.SendMessageAsync($"⚠️ ({tries}/{maxTries}) Update failed. Trying again in 10s...");
						System.Threading.Thread.Sleep(10000); // Sleep 10s
					}
				}
			}
			while (!success);

			Environment.Exit(0);
		}
	}
}
