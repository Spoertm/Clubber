namespace Clubber.Configuration
{
	public interface IConfig
	{
		public string WhyAreYou { get; }
		public string Prefix { get; }
		public ulong CheaterRoleId { get; }
		public ulong UnregisteredRoleId { get; }
		public ulong RoleAssignerRoleId { get; }
		public ulong ClubberExceptionsChannelId { get; }
		public ulong DatabaseBackupChannelId { get; }
		public ulong TestingChannelId { get; }
		public ulong RegisterChannelId { get; }
		public ulong DdPalsId { get; }
		public ulong CronUpdateChannelId { get; }
		public ulong DdPalsRegisterChannelId { get; }
		public ulong LbEntriesCacheChannelId { get; }
		public ulong DdNewsChannelId { get; }
	}
}
