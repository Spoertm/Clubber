﻿using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace Clubber.DdRoleUpdater
{
    public static class RoleUpdaterHelper
    {
        public static bool UserExists(ulong discordId)
        {
            return RoleUpdater.DdPlayerDatabase.ContainsKey(discordId);
        }

        public static bool MemberHasRole(SocketGuildUser member, ulong roleId)
        {
            foreach (SocketRole role in member.Roles)
            {
                if (role.Id == roleId)
                    return true;
            }
            return false;
        }

        public static List<SocketRole> RemoveScoreRoles(SocketGuildUser member)
        {
            List<SocketRole> removedRoles = new List<SocketRole>();

            foreach (var role in member.Roles)
            {
                if (RoleUpdater.ScoreRoleDict.ContainsValue(role.Id))
                {
                    member.RemoveRoleAsync(role);
                    removedRoles.Add(role);
                }
            }
            return removedRoles;
        }

        public static EmbedFieldBuilder BuildEmbed(string name, string value, bool inline)
        {
            return new EmbedFieldBuilder
            {
                Name = name,
                Value = value,
                IsInline = inline
            };
        }
    }
}
