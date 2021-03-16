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
	}
}
