using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Clubber.DdRoleUpdater
{
    public static class RoleUpdaterHelper
    
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

        public static List<SocketRole> RemoveScoreRolesExcept(SocketGuildUser member, SocketRole excludedRole)
        {
            List<SocketRole> removedRoles = new List<SocketRole>();

            foreach (var role in member.Roles)
            {
                if (RoleUpdater.ScoreRoleDict.ContainsValue(role.Id) && role.Id != excludedRole.Id)
                {
                    member.RemoveRoleAsync(role);
                    removedRoles.Add(role);
                }
            }
            return removedRoles;
        }

        public static EmbedFieldBuilder BuildField(string name, string value, bool inline)
        {
            return new EmbedFieldBuilder
            {
                Name = name,
                Value = value,
                IsInline = inline
            };
        }

        public static string GetCommandAndParameterString(CommandInfo cmd) // Returns the command and its params in the format: commandName <requiredParam> [optionalParam]
        {
            return $"{cmd.Name} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**[{p.Name}]**" : $"**[{p.Name} = {p.DefaultValue}]**" : $"**<{p.Name}>**"))}";
        }
    }
}
