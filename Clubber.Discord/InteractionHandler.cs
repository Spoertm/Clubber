using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Serilog;

namespace Clubber.Discord;

public class InteractionHandler
{
	private readonly AppConfig _config;
	private readonly UserService _userService;
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IDiscordHelper _discordHelper;
	private readonly RegistrationTracker _registrationTracker;

	public InteractionHandler(
		IOptions<AppConfig> config,
		UserService userService,
		IDatabaseHelper databaseHelper,
		IDiscordHelper discordHelper,
		RegistrationTracker registrationTracker)
	{
		_config = config.Value;
		_userService = userService;
		_databaseHelper = databaseHelper;
		_discordHelper = discordHelper;
		_registrationTracker = registrationTracker;
	}

	public async Task OnButtonExecuted(SocketMessageComponent component)
	{
		if (component.HasResponded)
		{
			return;
		}

		if (component.Data.CustomId.StartsWith("register"))
		{
			await HandleRegistration(component);
		}
		else if (component.Data.CustomId == "deny_button")
		{
			await component.Channel.SendMessageAsync(embeds: [new EmbedBuilder().WithDescription("ℹ️ Interaction closed.").Build()]);
			await ClearMessageComponents(component);
		}
	}

	private async Task HandleRegistration(SocketMessageComponent component)
	{
		if (!component.GuildId.HasValue)
		{
			await component.Channel.SendMessageAsync(embeds: [new EmbedBuilder().WithDescription("❌ Internal error.").Build()]);
			await ClearMessageComponents(component);
			return;
		}

		RegistrationContext regContext = RegistrationContext.Parse(component.Data.CustomId);

		SocketGuildUser? guildUser = _discordHelper.GetGuildUser(component.GuildId!.Value, regContext.UserId);
		if (guildUser is null) // User likely left the server
		{
			await component.Channel.SendMessageAsync(embeds: [new EmbedBuilder().WithDescription("⚠️ Couldn't find the user. They likely left the server.").Build()]);
			await ClearMessageComponents(component);
			return;
		}

		Result registrationResult = await CheckUserAndRegister(regContext.LeaderboardId, guildUser);
		if (registrationResult.IsFailure)
		{
			await component.Channel.SendMessageAsync(embeds: [new EmbedBuilder().WithDescription($"{registrationResult.ErrorMsg}").Build()]);
			await ClearMessageComponents(component);
			return;
		}

		_registrationTracker.UnflagUser(guildUser.Id);

		SocketTextChannel? registerChannel = null;
		try
		{
			registerChannel = _discordHelper.GetTextChannel(_config.RegisterChannelId);
		}
		catch (Exception e)
		{
			Log.Warning(e, "Failed to retrieve Register channel");
		}

		string modsSuccessEmbedDescription = "✅ Done!";
		if (registerChannel == null)
		{
			modsSuccessEmbedDescription += "\n\n⚠️ Register channel couldn't be found so the user couldn't be informed of their registration.";
		}
		else
		{
			string userSuccessMsg = regContext.LeaderboardId > 0
				? "✅ You've been registered!\n\nPlease do `+pb` anywhere to get the role."
				: "✅ Done!\n\nℹ️ Keep in mind you'll have limited channel access, but you can always ping the mods to get registered.";

			Embed userSuccessEmbed = new EmbedBuilder().WithDescription(userSuccessMsg).Build();
			if (await registerChannel.GetMessageAsync(regContext.RegisterMessageId) is IUserMessage userMsg)
			{
				await userMsg.ReplyAsync(embeds: [userSuccessEmbed]);
			}
			else
			{
				await registerChannel.SendMessageAsync(guildUser.Mention, embeds: [userSuccessEmbed]);
			}
		}

		Embed modsSuccessEmbed = new EmbedBuilder()
			.WithDescription(modsSuccessEmbedDescription)
			.Build();

		await ClearMessageComponents(component);
		await component.Channel.SendMessageAsync(embeds: [modsSuccessEmbed]);
	}

	private async Task ClearMessageComponents(SocketMessageComponent component)
	{
		if (await component.Channel.GetMessageAsync(component.Message.Id) is IUserMessage originalMessage)
		{
			await originalMessage.ModifyAsync(msg => msg.Components = new ComponentBuilder().Build());
		}
	}

	private async Task<Result> CheckUserAndRegister(int lbId, SocketGuildUser user)
	{
		Result result = await _userService.IsValidForRegistration(user, false);
		if (result.IsFailure)
		{
			return Result.Failure($"⚠️ {result.ErrorMsg}");
		}

		if (lbId < 0)
		{
			await user.AddRoleAsync(_config.NoScoreRoleId);
		}
		else
		{
			Result registrationResult = await _databaseHelper.RegisterUser((uint)lbId, user.Id);
			if (registrationResult.IsFailure)
			{
				return Result.Failure($"❌ Failed to execute command: {registrationResult.ErrorMsg}");
			}

			const ulong newPalRoleId = 728663492424499200;
			const ulong pendingPbRoleId = 994354086646399066;
			await user.RemoveRoleAsync(newPalRoleId);
			await user.AddRoleAsync(pendingPbRoleId);
		}

		return Result.Success();
	}

	private record struct RegistrationContext(ulong UserId, int LeaderboardId, ulong RegisterMessageId)
	{
		// a:b:c:d
		// a: "register"
		// b: user ID
		// c: leaderboard ID or "-1" for no score role
		// d: message ID in the registration channel
		public static RegistrationContext Parse(string input)
		{
			string[] data = input.Split(':');
			RegistrationContext regContext = new()
			{
				UserId = ulong.Parse(data[1]),
				LeaderboardId = int.Parse(data[2]),
				RegisterMessageId = ulong.Parse(data[3]),
			};

			return regContext;
		}
	}
}
