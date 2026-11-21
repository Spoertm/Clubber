using System.Collections.Immutable;
using Clubber.Domain.Configuration;
using Discord;
using NSubstitute;

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

    /// <summary>Builds a complete <see cref="AppConfig"/> without depending on appsettings files.</summary>
    public static AppConfig CreateAppConfig() => new()
    {
        BotToken = "SomeToken",
        FormerWrRoleId = 1383750696754483200,
        RegisterChannelId = 743463356710649866,
        RoleAssignerRoleId = 611855024653336606,
        CheaterRoleId = 666,
        DdPalsId = 743463356710649866,
        UnregisteredRoleId = 728663492424499200,
        DailyUpdateChannelId = 743463356710649866,
        DailyUpdateLoggingChannelId = 743463356710649866,
        DdNewsChannelId = 743463356710649866,
        ModsChannelId = 743463356710649866,
        NoScoreRoleId = 1266817935704588309,
        PendingPbRoleId = 994354086646399066,
        NewPalRoleId = 728663492424499200,
        Endpoints = new Endpoints
        {
            GetMultipleUsersById = new Uri("http://dd.hasmodai.com/dd3/get_multiple_users_by_id_public.php"),
            GetScores = new Uri("http://dd.hasmodai.com/dd3/get_scores.php"),
            GetWorldRecords = new Uri("https://devildaggers.info/api/world-records"),
            GetCountryCodeForPlayer = "https://devildaggers.info/api/clubber/players/",
            GetPlayerHistory = "https://devildaggers.info/api/clubber/players/",
            GetDdstatsResponse = "https://ddstats.com/api/v2/game/full",
            GetRecentScores = new Uri("https://dd.hasmodai.com/dd3/get_recent.php"),
        },
    };

    /// <summary>Creates a mocked guild user with a display name and the given roles.</summary>
    public static IGuildUser CreateGuildUser(ulong id, string name, IReadOnlyCollection<ulong> roleIds)
    {
        IGuildUser user = Substitute.For<IGuildUser>();
        user.Id.Returns(id);
        user.IsBot.Returns(false);
        user.GuildPermissions.Returns(GuildPermissions.None);
        user.RoleIds.Returns(roleIds);
        user.Nickname.Returns(name);
        user.GlobalName.Returns(name);
        user.Username.Returns(name);
        return user;
    }
}
