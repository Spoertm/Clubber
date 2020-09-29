using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Clubber.DdRoleUpdater
{
	public class DdUser
	{
		[BsonId]
		[BsonRepresentation(BsonType.Int64)]
		public ulong DiscordId { get; set; }
		[BsonRepresentation(BsonType.Int32)]
		public int LeaderboardId { get; set; }
		[BsonRepresentation(BsonType.Int32)]
		public int? Score { get; set; }

		public DdUser(ulong discordId, int leaderboardId)
		{
			DiscordId = discordId;
			LeaderboardId = leaderboardId;
		}
	}
}
