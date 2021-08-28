using System.Threading.Tasks;

namespace Clubber.Services
{
	public interface IIOService
	{
		T DeserializeObject<T>(string s);

		Task<T?> ReadObjectFromFile<T>(string filePath);

		Task WriteObjectToFile<T>(T tObject, string filePath);
	}
}
