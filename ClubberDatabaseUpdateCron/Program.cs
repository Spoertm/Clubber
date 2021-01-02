using System.Threading.Tasks;

namespace ClubberDatabaseUpdateCron
{
	public static class Program
	{
		public static Task Main() => new Startup().RunAsync();
	}
}
