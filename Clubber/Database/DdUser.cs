using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Clubber.Files
{
	public class DdUser
	{
		public DdUser(ulong discordId, int leaderboardId, int score)
		{
			DiscordId = discordId;
			LeaderboardId = leaderboardId;
			Score = score;
		}

		[BsonId]
		[BsonRepresentation(BsonType.Int64)]
		public ulong DiscordId { get; set; }
		[BsonRepresentation(BsonType.Int32)]
		public int LeaderboardId { get; set; }
		[BsonRepresentation(BsonType.Int32)]
		public int Score { get; set; }
	}
}
