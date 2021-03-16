using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Clubber.Helpers
{
	public static class EmbedHelper
	{
		public static Embed GetUpdateRolesEmbed(UpdateRolesResponse response)
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle($"Updated roles for {response.User!.Username}")
				.WithThumbnailUrl(response.User!.GetAvatarUrl() ?? response.User!.GetDefaultAvatarUrl());

			if (response.RolesRemoved!.Any())
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Removed:")
					.WithValue(string.Join('\n', response.RolesRemoved!.Select(rr => rr.Mention)))
					.WithIsInline(true));
			}

			if (response.RolesAdded!.Any())
			{
				embed.AddField(new EmbedFieldBuilder()
					.WithName("Added:")
					.WithValue(string.Join('\n', response.RolesAdded!.Select(ar => ar.Mention)))
					.WithIsInline(true));
			}

			return embed.Build();
		}

		public static Embed GetExceptionEmbed(LogMessage msg, SocketUserMessage userMessage)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle(msg.Exception?.GetType().Name ?? "Exception thrown")
				.AddField("Severity", msg.Severity, true)
				.AddField("Source", msg.Source, true)
				.AddField("User message", Format.Code(userMessage.Content))
				.WithCurrentTimestamp();

			Exception? ex = msg.Exception;

			string? exString = ex?.ToString();
			if (exString != null)
			{
				Match regexMatch = Regex.Match(exString, "(?<=   )at.+\n?", RegexOptions.Compiled);
				exceptionEmbed.AddField("Location", regexMatch.Value);
			}

			while (ex != null)
			{
				exceptionEmbed.AddField(ex.GetType().Name, ex.Message ?? "No message.");
				ex = ex.InnerException;
			}

			return exceptionEmbed.Build();
		}

		public static Embed GetExceptionEmbed(Exception? exception)
		{
			EmbedBuilder exceptionEmbed = new EmbedBuilder()
				.WithTitle("Cron project - " + exception?.GetType().Name ?? "Exception thrown")
				.WithCurrentTimestamp();

			string? exString = exception?.ToString();
			if (exString != null)
			{
				Match regexMatch = Regex.Match(exString, "(?<=   )at.+\n?", RegexOptions.Compiled);
				exceptionEmbed.AddField("Location", regexMatch.Value);
			}

			while (exception != null)
			{
				exceptionEmbed.AddField(exception.GetType().Name, exception.Message ?? "No message.");
				exception = exception.InnerException;
			}

			return exceptionEmbed.Build();
		}
	}
}
