using System.Collections.Frozen;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Clubber.Domain.Configuration;

public sealed class AppConfig
{
	public const ulong FormerWrRoleId = 1383750696754483200;

	public static ImmutableSortedDictionary<int, ulong> ScoreRoles { get; } = new Dictionary<int, ulong>
	{
		[1300] = 1046380614431019038,
		[1295] = 1310208049956388964,
		[1290] = 1046380561985454120,
		[1285] = 1310207791910096958,
		[1280] = 1046380490812313650,
		[1275] = 1310207802798768158,
		[1270] = 1046380385132617768,
		[1265] = 1310207806884024370,
		[1260] = 1046380324759814154,
		[1255] = 1310207811023536178,
		[1250] = 980126799075876874,
		[1240] = 980126039055429655,
		[1230] = 903024433315323915,
		[1220] = 903024200049102948,
		[1210] = 903023707121926195,
		[1200] = 626477161825697803,
		[1190] = 860585008394010634,
		[1180] = 728017461911355454,
		[1170] = 860584658373836800,
		[1160] = 626476794878623756,
		[1150] = 860584131368714292,
		[1140] = 626476562128044052,
		[1130] = 860583620761616384,
		[1120] = 525082045614129163,
		[1110] = 1155435926303035503,
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
	}.ToImmutableSortedDictionary(Comparer<int>.Create((x, y) => y.CompareTo(x))); // Descending

	public static ImmutableSortedDictionary<int, ulong> RankRoles => new Dictionary<int, ulong>
	{
		[1] = 446688666325090310,
		[3] = 472451008342261820,
		[10] = 556255819323277312,
		[25] = 992793365684949063,
	}.ToImmutableSortedDictionary();

	public static FrozenDictionary<int, string> DeathTypes { get; } = new Dictionary<int, string>
	{
		[0] = "FALLEN",
		[1] = "SWARMED",
		[2] = "IMPALED",
		[3] = "GORED",
		[4] = "INFESTED",
		[5] = "OPENED",
		[6] = "PURGED",
		[7] = "DESECRATED",
		[8] = "SACRIFICED",
		[9] = "EVISCERATED",
		[10] = "ANNIHILATED",
		[11] = "INTOXICATED",
		[12] = "ENVENOMATED",
		[13] = "INCARNATED",
		[14] = "DISCARNATED",
		[15] = "ENTANGLED",
		[16] = "HAUNTED",
	}.ToFrozenDictionary();

	[Required]
	public string Prefix { get; set; } = "+";

	[Required]
	public string BotToken { get; set; } = string.Empty;

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong ClubberLoggerId { get; set; }

	[Required]
	public string ClubberLoggerToken { get; set; } = string.Empty;

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong RegisterChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong RoleAssignerRoleId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong CheaterRoleId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong DdPalsId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong UnregisteredRoleId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong DailyUpdateChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong DailyUpdateLoggingChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong DdNewsChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong ModsChannelId { get; set; }

	[Required]
	[Range(1, ulong.MaxValue)]
	public ulong NoScoreRoleId { get; set; }
}
