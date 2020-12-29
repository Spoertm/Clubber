using Discord.Commands;
using System.Threading.Tasks;

namespace Clubber.Modules
{
	public abstract class AbstractModule<T> : ModuleBase<T>
		where T : class, ICommandContext
	{
		public async Task<bool> IsError(bool condition, string output)
		{
			if (condition)
			{
				await ReplyAsync(output);
				return true;
			}

			return false;
		}
	}
}
