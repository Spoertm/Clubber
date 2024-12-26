using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clubber.Domain.Database.Migrations
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
                name: "DdNewsEntryResponse",
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
                    table.PrimaryKey("PK_DdNewsEntryResponse", x => x.Id);
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
                name: "GameInfo",
                schema: "clubber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerName = table.Column<string>(type: "text", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    PlayerGameTime = table.Column<int>(type: "integer", nullable: false),
                    Granularity = table.Column<int>(type: "integer", nullable: false),
                    GameTime = table.Column<float>(type: "real", nullable: false),
                    DeathType = table.Column<string>(type: "text", nullable: false),
                    Gems = table.Column<int>(type: "integer", nullable: false),
                    HomingDaggers = table.Column<int>(type: "integer", nullable: false),
                    DaggersFired = table.Column<int>(type: "integer", nullable: false),
                    DaggersHit = table.Column<int>(type: "integer", nullable: false),
                    Accuracy = table.Column<double>(type: "double precision", nullable: false),
                    EnemiesAlive = table.Column<int>(type: "integer", nullable: false),
                    EnemiesKilled = table.Column<int>(type: "integer", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "integer", nullable: false),
                    ReplayPlayerName = table.Column<string>(type: "text", nullable: true),
                    Spawnset = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    LevelTwoTime = table.Column<double>(type: "double precision", nullable: false),
                    LevelThreeTime = table.Column<double>(type: "double precision", nullable: false),
                    LevelFourTime = table.Column<double>(type: "double precision", nullable: false),
                    LeviDownTime = table.Column<double>(type: "double precision", nullable: false),
                    OrbDownTime = table.Column<double>(type: "double precision", nullable: false),
                    HomingDaggersMaxTime = table.Column<double>(type: "double precision", nullable: false),
                    EnemiesAliveMaxTime = table.Column<double>(type: "double precision", nullable: false),
                    HomingDaggersMax = table.Column<int>(type: "integer", nullable: false),
                    EnemiesAliveMax = table.Column<int>(type: "integer", nullable: false),
                    IsReplay = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameInfo", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "DdNews",
                schema: "clubber",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    LeaderboardId = table.Column<int>(type: "integer", nullable: false),
                    OldEntryId = table.Column<int>(type: "integer", nullable: false),
                    NewEntryId = table.Column<int>(type: "integer", nullable: false),
                    TimeOfOccurenceUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdNews", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_DdNews_DdNewsEntryResponse_NewEntryId",
                        column: x => x.NewEntryId,
                        principalSchema: "clubber",
                        principalTable: "DdNewsEntryResponse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdNews_DdNewsEntryResponse_OldEntryId",
                        column: x => x.OldEntryId,
                        principalSchema: "clubber",
                        principalTable: "DdNewsEntryResponse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BestSplits",
                schema: "clubber",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    GameInfoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BestSplits", x => x.Name);
                    table.ForeignKey(
                        name: "FK_BestSplits_GameInfo_GameInfoId",
                        column: x => x.GameInfoId,
                        principalSchema: "clubber",
                        principalTable: "GameInfo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BestSplits_GameInfoId",
                schema: "clubber",
                table: "BestSplits",
                column: "GameInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_DdNews_NewEntryId",
                schema: "clubber",
                table: "DdNews",
                column: "NewEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_DdNews_OldEntryId",
                schema: "clubber",
                table: "DdNews",
                column: "OldEntryId");
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
                name: "TopHomingPeaks",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "GameInfo",
                schema: "clubber");

            migrationBuilder.DropTable(
                name: "DdNewsEntryResponse",
                schema: "clubber");
        }
    }
}
