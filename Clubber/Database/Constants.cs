using System.Collections.Generic;

namespace Clubber
{
	public static class Constants
	{
		public const string WhyAreYou = @"Every day or so, I automatically update people's DD roles.
For example, if someone beats their score of 300s and gets 400s, I update their role from `300+ club` to `400+ club`.
To speed this up, you can manually update your own or someone else's roles by using the `updateroles` command.";

		public const string Prefix = "+";
		public const string Token = "NzQzNDMxNTAyODQyMjk4MzY4.XzUkig.UQrKlF7axeeFqewonpkTTAwaIIo";
		public const ulong CheaterRoleId = 693432614727581727;
		public const ulong WrRoleId = 446688666325090310;
		public const ulong Top3RoleId = 472451008342261820;
		public const ulong Top10RoleId = 556255819323277312;
		public const ulong ClubberExceptionsChannel = 800012212532936714;
		public const ulong TestingChannel = 447487662891466752;
		public const ulong DdPals = 399568958669455364;

		public static readonly List<ulong> UselessRoles = new() { 728663492424499200, 458375331468935178 };

		public static readonly Dictionary<int, ulong> ScoreRoles = new()
		{
			[1200] = 626477161825697803,
			[1180] = 728017461911355454,
			[1160] = 626476794878623756,
			[1140] = 626476562128044052,
			[1120] = 525082045614129163,
			[1100] = 402530230109208577,
			[1075] = 525733825934786570,
			[1050] = 399577125180669963,
			[1025] = 525967813551325196,
			[1000] = 399570979610820608,
			[950] = 728017240762482788,
			[900] = 399570895741386765,
			[800] = 399570790506299398,
			[700] = 399570712018288640,
			[600] = 399569864261632001,
			[500] = 399569581561217024,
			[400] = 399569447771439104,
			[300] = 399569332532674562,
			[200] = 399569259182948363,
			[100] = 399569183966363648,
			[0] = 461203024128376832,
		};

		public static readonly Dictionary<int, ulong> RankRoles = new()
		{
			[1] = 446688666325090310,
			[3] = 472451008342261820,
			[10] = 556255819323277312,
		};
	}
}
