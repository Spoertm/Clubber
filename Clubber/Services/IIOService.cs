using System.Threading.Tasks;

namespace Clubber.Services
{
	public interface IIOService
	{
		Task<T?> ReadObjectFromFile<T>(string filePath);

		Task WriteObjectToFile<T>(T tObject, string filePath);
	}
}
