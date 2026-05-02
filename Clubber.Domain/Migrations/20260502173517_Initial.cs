using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clubber.Domain.Migrations
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
                    GameInfo_PlayerId = table.Column<long>(type: "bigint", nullable: true),
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
                    LeaderboardId = table.Column<long>(type: "bigint", nullable: false),
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
                    NewEntry_Id = table.Column<long>(type: "bigint", nullable: false),
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
                    OldEntry_Id = table.Column<long>(type: "bigint", nullable: false),
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
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LeaderboardId = table.Column<long>(type: "bigint", nullable: false),
                    TwitchUsername = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdPlayers", x => x.DiscordId);
                });

            migrationBuilder.CreateTable(
                name: "HundredthCounts",
                schema: "clubber",
                columns: table => new
                {
                    Threshold = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HundredthCounts", x => x.Threshold);
                });

            migrationBuilder.CreateTable(
                name: "NewsCursors",
                schema: "clubber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsCursors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPbs",
                schema: "clubber",
                columns: table => new
                {
                    LeaderboardId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPbs", x => x.LeaderboardId);
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
                    PlayerLeaderboardId = table.Column<long>(type: "bigint", nullable: false),
                    HomingPeak = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopHomingPeaks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DdPlayers_LeaderboardId",
                schema: "clubber",
                table: "DdPlayers",
                column: "LeaderboardId");
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
                name: "HundredthCounts",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "NewsCursors",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "PlayerPbs",
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
