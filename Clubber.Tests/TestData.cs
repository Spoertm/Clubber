using System.Collections.Immutable;

namespace Clubber.Tests;

/// <summary>
/// Test data for roles - matches the production seed data.
/// </summary>
public static class TestData
{
    public static readonly ImmutableSortedDictionary<int, ulong> ScoreRoles = new Dictionary<int, ulong>
    {
        [1300] = 1300,
        [1295] = 1295,
        [1290] = 1290,
        [1285] = 1285,
        [1280] = 1280,
        [1275] = 1275,
        [1270] = 1270,
        [1265] = 1265,
        [1260] = 1260,
        [1255] = 1255,
        [1250] = 1250,
        [1240] = 1240,
        [1230] = 1230,
        [1220] = 1220,
        [1210] = 1210,
        [1200] = 1200,
        [1190] = 1190,
        [1180] = 1180,
        [1170] = 1170,
        [1160] = 1160,
        [1150] = 1150,
        [1140] = 1140,
        [1130] = 1130,
        [1120] = 1120,
        [1110] = 1110,
        [1100] = 1100,
        [1075] = 1075,
        [1050] = 1050,
        [1025] = 1025,
        [1000] = 1000,
        [950] = 950,
        [900] = 900,
        [800] = 800,
        [700] = 700,
        [600] = 600,
        [500] = 500,
        [400] = 400,
        [300] = 300,
        [200] = 200,
        [100] = 100,
        [0] = 0,
    }.ToImmutableSortedDictionary(Comparer<int>.Create((x, y) => y.CompareTo(x)));

    public static readonly ImmutableSortedDictionary<int, ulong> RankRoles = new Dictionary<int, ulong>
    {
        [1] = 1,
        [3] = 3,
        [10] = 10,
        [25] = 25,
    }.ToImmutableSortedDictionary();
}
