using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models.DdSplits;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using ModuleInfo = Discord.Interactions.ModuleInfo;
using PreconditionResult = Discord.Interactions.PreconditionResult;

namespace Clubber.Discord.Modules;

[Name("ℹ️ Information")]
public sealed class InfoCommands(IDatabaseHelper databaseHelper, ClubberDiscordClient discordClient, IServiceProvider serviceProvider)
	: InteractionModuleBase<SocketInteractionContext>
{
	[SlashCommand("help", "Get help information about available commands")]
	public async Task Help()
	{
		EmbedBuilder embed = new EmbedBuilder()
			.WithTitle("Clubber Slash Commands")
			.WithDescription("Here are the available slash commands:")
			.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
			.WithColor(Color.Blue)
			.WithFooter("Use Discord's built-in slash command autocomplete to see parameter details.");

		Dictionary<string, List<SlashCommandInfo>> commandsByModule = new();
		InteractionService interactionService = discordClient.GetInteractionService();
		foreach (ModuleInfo module in interactionService.Modules)
		{
			foreach (SlashCommandInfo? command in module.SlashCommands)
			{
				PreconditionResult? preconditionResult = await command.CheckPreconditionsAsync(Context, serviceProvider);
				if (preconditionResult.IsSuccess)
				{
					string moduleName = module.Attributes
						.OfType<NameAttribute>()
						.FirstOrDefault()?.Text ?? module.Name;

					if (!commandsByModule.ContainsKey(moduleName))
						commandsByModule[moduleName] = [];

					commandsByModule[moduleName].Add(command);
				}
			}
		}

		foreach (KeyValuePair<string, List<SlashCommandInfo>> kvp in commandsByModule)
		{
			if (kvp.Value.Count == 0) continue;

			List<string> commandList = kvp.Value
				.OrderBy(cmd => cmd.Name)
				.Select(cmd => $"* `/{cmd.Name}` - {cmd.Description}")
				.ToList();

			embed.AddField(kvp.Key, string.Join('\n', commandList));
		}

		await RespondAsync(embed: embed.Build());
	}

	[SlashCommand("bestsplits", "Get the current best Devil Daggers splits")]
	public async Task CurrentBestSplits()
	{
		await DeferAsync();

		try
		{
			BestSplit[] bestSplits = await databaseHelper.GetBestSplits();
			Embed bestSplitsEmbed = EmbedHelper.CurrentBestSplits(bestSplits);
			await FollowupAsync(embed: bestSplitsEmbed);
		}
		catch (Exception ex)
		{
			Serilog.Log.Error(ex, "Error fetching best splits");
			await FollowupAsync("Failed to fetch best splits.", ephemeral: true);
		}
	}

	[SlashCommand("toppeaks", "Get the current best homing peaks")]
	public async Task CurrentTopHomingPeaks()
	{
		await DeferAsync();

		try
		{
			HomingPeakRun[] topHomingPeaks = await databaseHelper.GetTopHomingPeaks();
			Embed topPeaksEmbed = EmbedHelper.CurrentTopPeakRuns(topHomingPeaks);
			await FollowupAsync(embed: topPeaksEmbed);
		}
		catch (Exception ex)
		{
			Serilog.Log.Error(ex, "Error fetching top homing peaks");
			await FollowupAsync("Failed to fetch top homing peaks.", ephemeral: true);
		}
	}
}
