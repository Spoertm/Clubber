using System.Text.Json.Serialization;

namespace Clubber.Domain.Models.DdSplits;

public class Split
{
	public Split(string name, int time, int value)
	{
		Name = name;
		Time = time;
		Value = value;
	}

	public string Name { get; }
	public int Time { get; }

	public int Value { get; set; }

	[JsonIgnore]
	public static (string Name, int Time)[] V3Splits =>
	[
		("350", 366),
		("700", 709),
		("800", 800),
		("880", 875),
		("930", 942),
		("1000", 996),
		("1040", 1047),
		("1080", 1091),
		("1130", 1133),
		("1160", 1170),
		("1200", 1203),
		("1230", 1231),
		("1260", 1260),
		("1290", 1290),
	];
}
