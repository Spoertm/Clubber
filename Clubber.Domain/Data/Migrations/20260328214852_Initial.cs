using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clubber.Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clubber");

            migrationBuilder.CreateTable(
                name: "BestSplits",
                schema: "clubber",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    GameInfo_Accuracy = table.Column<double>(type: "double precision", nullable: true),
                    GameInfo_DaggersFired = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_DaggersHit = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_DeathType = table.Column<string>(type: "text", nullable: true),
                    GameInfo_EnemiesAlive = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_EnemiesAliveMax = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_EnemiesAliveMaxTime = table.Column<double>(type: "double precision", nullable: true),
                    GameInfo_EnemiesKilled = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_GameTime = table.Column<float>(type: "real", nullable: true),
                    GameInfo_Gems = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_Granularity = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_HomingDaggers = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_HomingDaggersMax = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_HomingDaggersMaxTime = table.Column<double>(type: "double precision", nullable: true),
                    GameInfo_Id = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_IsReplay = table.Column<bool>(type: "boolean", nullable: true),
                    GameInfo_LevelFourTime = table.Column<double>(type: "double precision", nullable: true),
                    GameInfo_LevelThreeTime = table.Column<double>(type: "double precision", nullable: true),
                    GameInfo_LevelTwoTime = table.Column<double>(type: "double precision", nullable: true),
                    GameInfo_LeviDownTime = table.Column<double>(type: "double precision", nullable: true),
                    GameInfo_OrbDownTime = table.Column<double>(type: "double precision", nullable: true),
                    GameInfo_PlayerGameTime = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_PlayerId = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_PlayerName = table.Column<string>(type: "text", nullable: true),
                    GameInfo_ReplayPlayerId = table.Column<int>(type: "integer", nullable: true),
                    GameInfo_ReplayPlayerName = table.Column<string>(type: "text", nullable: true),
                    GameInfo_Spawnset = table.Column<string>(type: "text", nullable: true),
                    GameInfo_TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GameInfo_Version = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BestSplits", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "DdNews",
                schema: "clubber",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    LeaderboardId = table.Column<int>(type: "integer", nullable: false),
                    TimeOfOccurenceUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nth = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_DaggersFired = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_DaggersFiredTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NewEntry_DaggersHit = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_DaggersHitTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NewEntry_DeathType = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_DeathsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NewEntry_Gems = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_GemsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NewEntry_Id = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_Kills = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_KillsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NewEntry_Rank = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_Time = table.Column<int>(type: "integer", nullable: false),
                    NewEntry_TimeTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NewEntry_Username = table.Column<string>(type: "text", nullable: false),
                    OldEntry_DaggersFired = table.Column<int>(type: "integer", nullable: false),
                    OldEntry_DaggersFiredTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OldEntry_DaggersHit = table.Column<int>(type: "integer", nullable: false),
                    OldEntry_DaggersHitTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OldEntry_DeathType = table.Column<int>(type: "integer", nullable: false),
                    OldEntry_DeathsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OldEntry_Gems = table.Column<int>(type: "integer", nullable: false),
                    OldEntry_GemsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OldEntry_Id = table.Column<int>(type: "integer", nullable: false),
                    OldEntry_Kills = table.Column<int>(type: "integer", nullable: false),
                    OldEntry_KillsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OldEntry_Rank = table.Column<int>(type: "integer", nullable: false),
                    OldEntry_Time = table.Column<int>(type: "integer", nullable: false),
                    OldEntry_TimeTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OldEntry_Username = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdNews", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "DdPlayers",
                schema: "clubber",
                columns: table => new
                {
                    LeaderboardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TwitchUsername = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdPlayers", x => x.LeaderboardId);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardCache",
                schema: "clubber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<int>(type: "integer", nullable: false),
                    Kills = table.Column<int>(type: "integer", nullable: false),
                    Gems = table.Column<int>(type: "integer", nullable: false),
                    DeathType = table.Column<int>(type: "integer", nullable: false),
                    DaggersHit = table.Column<int>(type: "integer", nullable: false),
                    DaggersFired = table.Column<int>(type: "integer", nullable: false),
                    TimeTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    KillsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GemsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DeathsTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DaggersHitTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DaggersFiredTotal = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RankRoles",
                schema: "clubber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DiscordRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScoreRoles",
                schema: "clubber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DiscordRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopHomingPeaks",
                schema: "clubber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerName = table.Column<string>(type: "text", nullable: false),
                    PlayerLeaderboardId = table.Column<int>(type: "integer", nullable: false),
                    HomingPeak = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopHomingPeaks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BestSplits",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "DdNews",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "DdPlayers",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "LeaderboardCache",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "RankRoles",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "ScoreRoles",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "TopHomingPeaks",
                schema: "clubber");
        }
    }
}
