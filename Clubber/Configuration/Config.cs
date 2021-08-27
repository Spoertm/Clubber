using System.Configuration;

namespace Clubber.Configuration
{
	public static class Config
	{
		public static string WhyAreYou => ConfigurationManager.AppSettings["WhyAreYou"]!;
		public static string Prefix => ConfigurationManager.AppSettings["Prefix"]!;
		public static ulong CheaterRoleId => ulong.Parse(ConfigurationManager.AppSettings["CheaterRoleId"]!);
		public static ulong UnregisteredRoleId => ulong.Parse(ConfigurationManager.AppSettings["UnregisteredRoleId"]!);
		public static ulong RoleAssignerRoleId => ulong.Parse(ConfigurationManager.AppSettings["RoleAssignerRoleId"]!);
		public static ulong ClubberExceptionsChannelId => ulong.Parse(ConfigurationManager.AppSettings["ClubberExceptionsChannelId"]!);
		public static ulong DatabaseBackupChannelId => ulong.Parse(ConfigurationManager.AppSettings["DatabaseBackupChannelId"]!);
		public static ulong TestingChannelId => ulong.Parse(ConfigurationManager.AppSettings["TestingChannelId"]!);
		public static ulong RegisterChannelId => ulong.Parse(ConfigurationManager.AppSettings["RegisterChannelId"]!);
		public static ulong DdPalsId => ulong.Parse(ConfigurationManager.AppSettings["DdPalsId"]!);
		public static ulong CronUpdateChannelId => ulong.Parse(ConfigurationManager.AppSettings["CronUpdateChannelId"]!);
		public static ulong DdPalsRegisterChannelId => ulong.Parse(ConfigurationManager.AppSettings["DdPalsRegisterChannelId"]!);
		public static ulong LbEntriesCacheChannelId => ulong.Parse(ConfigurationManager.AppSettings["LbEntriesCacheChannelId"]!);
		public static ulong DdNewsChannelId => ulong.Parse(ConfigurationManager.AppSettings["DdNewsChannelId"]!);
	}
}
