using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Clubber.Services
{
	public class IOService : IIOService
	{
		public T DeserializeObject<T>(string s)
			=> JsonConvert.DeserializeObject<T>(s);

		public async Task<T?> ReadObjectFromFile<T>(string filePath)
		{
			if (!File.Exists(filePath))
				return default;

			string dbString = await File.ReadAllTextAsync(filePath);
			return JsonConvert.DeserializeObject<T>(dbString);
		}

		public async Task WriteObjectToFile<T>(T tObject, string filePath)
		{
			string fileContents = JsonConvert.SerializeObject(tObject, Formatting.Indented);
			await File.WriteAllTextAsync(filePath, fileContents);
		}
	}
}
