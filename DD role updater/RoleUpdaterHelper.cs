using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Clubber.DdRoleUpdater
{
    public static class RoleUpdaterHelper
    {
        private static readonly string DbJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DD role updater/DdPlayerDataBase.json");

        public static Dictionary<ulong, DdUser> DeserializeDb()
        {
            return JsonConvert.DeserializeObject<Dictionary<ulong, DdUser>>(File.ReadAllText(DbJsonPath));
        }

        public static bool UserExistsInDb(ulong discordId)
        {
            return DeserializeDb().ContainsKey(discordId);
        }

        public static bool MemberHasRole(SocketGuildUser member, ulong roleId)
        {
            return member.Roles.Any(role => role.Id == roleId);
        }

        public static string GetCommandAndParameterString(CommandInfo cmd) // Returns the command and its params in the format: commandName <requiredParam> [optionalParam]
        {
            return $"{cmd.Name} {string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? p.DefaultValue == null ? $"**[{p.Name}]**" : $"**[{p.Name} = {p.DefaultValue}]**" : $"**<{p.Name}>**"))}";
        }
    }
}
