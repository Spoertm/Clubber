using System.Threading.Tasks;

namespace ClubberDatabaseUpdateCron
{
	class Program
	{
		public static Task Main(string[] args) => Startup.RunAsync(args);
	}
}
