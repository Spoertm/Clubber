using Clubber.Files;
using MongoDB.Driver;

namespace Clubber.Databases
{
	public class MongoDatabase
	{
		public IMongoCollection<DdUser> DdUserCollection { get; }

		public MongoDatabase()
		{
			MongoClient client = new MongoClient("mongodb+srv://Ali_Alradwy:cEdM5Br52RYlbHaX@cluster0.ffrfn.mongodb.net/Clubber?retryWrites=true&w=majority");
			IMongoDatabase db = client.GetDatabase("Clubber");

			DdUserCollection = db.GetCollection<DdUser>("DdUsers");
		}
	}
}