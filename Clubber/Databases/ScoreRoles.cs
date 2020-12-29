using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Clubber.Databases
{
	public class ScoreRoles
	{
		public ScoreRoles()
		{
			string scoreRoleJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Files/ScoreRoles.json");
			ScoreRoleDictionary = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(File.ReadAllText(scoreRoleJsonPath));
		}

		public Dictionary<int, ulong> ScoreRoleDictionary { get; }
	}
}
