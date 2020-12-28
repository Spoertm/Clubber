using System.Threading.Tasks;

namespace ClubberDatabaseUpdateCron
{
	public static class Program
	{
		public static Task Main(string[] args) => Startup.RunAsync(args);
	}
}
