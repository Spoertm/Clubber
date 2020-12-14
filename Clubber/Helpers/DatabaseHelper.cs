using Clubber.Databases;
using Clubber.Files;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clubber.Helpers
{
	public class DatabaseHelper
	{
		private readonly MongoDatabase _database;

		public DatabaseHelper(MongoDatabase database)
		{
			_database = database;
		}

		public List<DdUser> GetUsers()
			=> _database.DdUserCollection.AsQueryable().ToList();

		public void FindAndUpdateUser(ulong discordId, int newScore)
			=> _database.DdUserCollection.FindOneAndUpdate(du => du.DiscordId == discordId, Builders<DdUser>.Update.Set(du => du.Score, newScore));

		public DdUser GetDdUserFromId(ulong discordId)
			=> _database.DdUserCollection.Find(du => du.DiscordId == discordId).SingleOrDefault();

		public bool DiscordIdExistsInDb(ulong discordId)
			=> _database.DdUserCollection.Find(du => du.DiscordId == discordId).Any();

		public bool LeaderboardIdExistsInDb(int lbId)
			=> _database.DdUserCollection.Find(du => du.LeaderboardId == lbId).Any();

		public bool MemberHasRole(SocketGuildUser member, ulong roleId)
			=> member.Roles.Any(role => role.Id == roleId);

		public void AddUser(DdUser ddUser)
			=> _database.DdUserCollection.InsertOne(ddUser);

		public record AddToDbFromNameResponse(int NumberOfMatches);

		public async Task<AddToDbFromNameResponse> AddToDbFromName(IEnumerable<SocketGuildUser> userMatches, uint rankOrLbId, Func<uint, ulong, Task> asyncCommand)
		{
			int numberOfMatches = userMatches.Count();
			if (numberOfMatches == 1)
				await asyncCommand(rankOrLbId, userMatches.First().Id);

			return new(numberOfMatches);
		}
	}
}
