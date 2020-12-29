using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Clubber.Databases
{
	public class ScoreRoles
	{
		public Dictionary<int, ulong> ScoreRoleDictionary { get; }

		public ScoreRoles()
		{
			string ScoreRoleJsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Files/ScoreRoles.json");
			ScoreRoleDictionary = JsonConvert.DeserializeObject<Dictionary<int, ulong>>(File.ReadAllText(ScoreRoleJsonPath));
		}
	}
}
