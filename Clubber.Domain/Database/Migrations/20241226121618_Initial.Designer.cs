﻿// <auto-generated />
using System;
using Clubber.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clubber.Domain.Database.Migrations
{
    [DbContext(typeof(ClubberContext))]
    [Migration("20241226121618_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("clubber")
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Clubber.Domain.Models.DdSplits.BestSplit", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("GameInfoId")
                        .HasColumnType("integer");

                    b.Property<int>("Time")
                        .HasColumnType("integer");

                    b.Property<int>("Value")
                        .HasColumnType("integer");

                    b.HasKey("Name");

                    b.HasIndex("GameInfoId");

                    b.ToTable("BestSplits", "clubber");
                });

            modelBuilder.Entity("Clubber.Domain.Models.DdSplits.HomingPeakRun", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("HomingPeak")
                        .HasColumnType("integer");

                    b.Property<int>("PlayerLeaderboardId")
                        .HasColumnType("integer");

                    b.Property<string>("PlayerName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("TopHomingPeaks", "clubber");
                });

            modelBuilder.Entity("Clubber.Domain.Models.DdUser", b =>
                {
                    b.Property<int>("LeaderboardId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("LeaderboardId"));

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("TwitchUsername")
                        .HasColumnType("text");

                    b.HasKey("LeaderboardId");

                    b.ToTable("DdPlayers", "clubber");
                });

            modelBuilder.Entity("Clubber.Domain.Models.Responses.DdNewsEntryResponse", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("DaggersFired")
                        .HasColumnType("integer");

                    b.Property<decimal>("DaggersFiredTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("DaggersHit")
                        .HasColumnType("integer");

                    b.Property<decimal>("DaggersHitTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("DeathType")
                        .HasColumnType("integer");

                    b.Property<decimal>("DeathsTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Gems")
                        .HasColumnType("integer");

                    b.Property<decimal>("GemsTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Kills")
                        .HasColumnType("integer");

                    b.Property<decimal>("KillsTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Rank")
                        .HasColumnType("integer");

                    b.Property<int>("Time")
                        .HasColumnType("integer");

                    b.Property<decimal>("TimeTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("DdNewsEntryResponse", "clubber");
                });

            modelBuilder.Entity("Clubber.Domain.Models.Responses.DdNewsItem", b =>
                {
                    b.Property<int>("ItemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("ItemId"));

                    b.Property<int>("LeaderboardId")
                        .HasColumnType("integer");

                    b.Property<int>("NewEntryId")
                        .HasColumnType("integer");

                    b.Property<int>("Nth")
                        .HasColumnType("integer");

                    b.Property<int>("OldEntryId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("TimeOfOccurenceUtc")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("ItemId");

                    b.HasIndex("NewEntryId");

                    b.HasIndex("OldEntryId");

                    b.ToTable("DdNews", "clubber");
                });

            modelBuilder.Entity("Clubber.Domain.Models.Responses.EntryResponse", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("DaggersFired")
                        .HasColumnType("integer");

                    b.Property<decimal>("DaggersFiredTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("DaggersHit")
                        .HasColumnType("integer");

                    b.Property<decimal>("DaggersHitTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("DeathType")
                        .HasColumnType("integer");

                    b.Property<decimal>("DeathsTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Gems")
                        .HasColumnType("integer");

                    b.Property<decimal>("GemsTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Kills")
                        .HasColumnType("integer");

                    b.Property<decimal>("KillsTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Rank")
                        .HasColumnType("integer");

                    b.Property<int>("Time")
                        .HasColumnType("integer");

                    b.Property<decimal>("TimeTotal")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("LeaderboardCache", "clubber");
                });

            modelBuilder.Entity("Clubber.Domain.Models.Responses.GameInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<double>("Accuracy")
                        .HasColumnType("double precision")
                        .HasAnnotation("Relational:JsonPropertyName", "accuracy");

                    b.Property<int>("DaggersFired")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "daggers_fired");

                    b.Property<int>("DaggersHit")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "daggers_hit");

                    b.Property<string>("DeathType")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "death_type");

                    b.Property<int>("EnemiesAlive")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "enemies_alive");

                    b.Property<int>("EnemiesAliveMax")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "enemies_alive_max");

                    b.Property<double>("EnemiesAliveMaxTime")
                        .HasColumnType("double precision")
                        .HasAnnotation("Relational:JsonPropertyName", "enemies_alive_max_time");

                    b.Property<int>("EnemiesKilled")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "enemies_killed");

                    b.Property<float>("GameTime")
                        .HasColumnType("real")
                        .HasAnnotation("Relational:JsonPropertyName", "game_time");

                    b.Property<int>("Gems")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "gems");

                    b.Property<int>("Granularity")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "granularity");

                    b.Property<int>("HomingDaggers")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "homing_daggers");

                    b.Property<int>("HomingDaggersMax")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "homing_daggers_max");

                    b.Property<double>("HomingDaggersMaxTime")
                        .HasColumnType("double precision")
                        .HasAnnotation("Relational:JsonPropertyName", "homing_daggers_max_time");

                    b.Property<bool>("IsReplay")
                        .HasColumnType("boolean")
                        .HasAnnotation("Relational:JsonPropertyName", "is_replay");

                    b.Property<double>("LevelFourTime")
                        .HasColumnType("double precision")
                        .HasAnnotation("Relational:JsonPropertyName", "level_four_time");

                    b.Property<double>("LevelThreeTime")
                        .HasColumnType("double precision")
                        .HasAnnotation("Relational:JsonPropertyName", "level_three_time");

                    b.Property<double>("LevelTwoTime")
                        .HasColumnType("double precision")
                        .HasAnnotation("Relational:JsonPropertyName", "level_two_time");

                    b.Property<double>("LeviDownTime")
                        .HasColumnType("double precision")
                        .HasAnnotation("Relational:JsonPropertyName", "levi_down_time");

                    b.Property<double>("OrbDownTime")
                        .HasColumnType("double precision")
                        .HasAnnotation("Relational:JsonPropertyName", "orb_down_time");

                    b.Property<int>("PlayerGameTime")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "player_game_time");

                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "player_id");

                    b.Property<string>("PlayerName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "player_name");

                    b.Property<int>("ReplayPlayerId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "replay_player_id");

                    b.Property<string>("ReplayPlayerName")
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "replay_player_name");

                    b.Property<string>("Spawnset")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "spawnset");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp with time zone")
                        .HasAnnotation("Relational:JsonPropertyName", "time_stamp");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "version");

                    b.HasKey("Id");

                    b.ToTable("GameInfo", "clubber");
                });

            modelBuilder.Entity("Clubber.Domain.Models.DdSplits.BestSplit", b =>
                {
                    b.HasOne("Clubber.Domain.Models.Responses.GameInfo", "GameInfo")
                        .WithMany()
                        .HasForeignKey("GameInfoId");

                    b.Navigation("GameInfo");
                });

            modelBuilder.Entity("Clubber.Domain.Models.Responses.DdNewsItem", b =>
                {
                    b.HasOne("Clubber.Domain.Models.Responses.DdNewsEntryResponse", "NewEntry")
                        .WithMany()
                        .HasForeignKey("NewEntryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clubber.Domain.Models.Responses.DdNewsEntryResponse", "OldEntry")
                        .WithMany()
                        .HasForeignKey("OldEntryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("NewEntry");

                    b.Navigation("OldEntry");
                });
#pragma warning restore 612, 618
        }
    }
}