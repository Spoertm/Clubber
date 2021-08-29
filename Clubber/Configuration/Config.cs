using System.Configuration;

#pragma warning disable 8603, 8604

namespace Clubber.Configuration
{
	public class Config : IConfig
	{
		public string WhyAreYou => ConfigurationManager.AppSettings["WhyAreYou"];
		public string Prefix => ConfigurationManager.AppSettings["Prefix"];
		public ulong CheaterRoleId => ulong.Parse(ConfigurationManager.AppSettings["CheaterRoleId"]);
		public ulong UnregisteredRoleId => ulong.Parse(ConfigurationManager.AppSettings["UnregisteredRoleId"]);
		public ulong RoleAssignerRoleId => ulong.Parse(ConfigurationManager.AppSettings["RoleAssignerRoleId"]);
		public ulong ClubberExceptionsChannelId => ulong.Parse(ConfigurationManager.AppSettings["ClubberExceptionsChannelId"]);
		public ulong DatabaseBackupChannelId => ulong.Parse(ConfigurationManager.AppSettings["DatabaseBackupChannelId"]);
		public ulong TestingChannelId => ulong.Parse(ConfigurationManager.AppSettings["TestingChannelId"]);
		public ulong RegisterChannelId => ulong.Parse(ConfigurationManager.AppSettings["RegisterChannelId"]);
		public ulong DdPalsId => ulong.Parse(ConfigurationManager.AppSettings["DdPalsId"]);
		public ulong CronUpdateChannelId => ulong.Parse(ConfigurationManager.AppSettings["CronUpdateChannelId"]);
		public ulong DdPalsRegisterChannelId => ulong.Parse(ConfigurationManager.AppSettings["DdPalsRegisterChannelId"]);
		public ulong LbEntriesCacheChannelId => ulong.Parse(ConfigurationManager.AppSettings["LbEntriesCacheChannelId"]);
		public ulong DdNewsChannelId => ulong.Parse(ConfigurationManager.AppSettings["DdNewsChannelId"]);
	}
}
