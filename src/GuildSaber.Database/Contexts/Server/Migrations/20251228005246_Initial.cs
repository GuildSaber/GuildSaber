using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameModes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameModes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    DiscordInfo_MainDiscordGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Info_Color = table.Column<int>(type: "integer", nullable: false),
                    Info_CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Info_Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Info_Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Info_SmallName = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Requirements_AccountAgeUnix = table.Column<int>(type: "integer", nullable: true),
                    Requirements_MaxPP = table.Column<int>(type: "integer", nullable: true),
                    Requirements_MaxRank = table.Column<int>(type: "integer", nullable: true),
                    Requirements_MinPP = table.Column<int>(type: "integer", nullable: true),
                    Requirements_MinRank = table.Column<int>(type: "integer", nullable: true),
                    Requirements_RequireSubmission = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsManager = table.Column<bool>(type: "boolean", nullable: false),
                    HardwareInfo_HMD = table.Column<int>(type: "integer", nullable: false),
                    HardwareInfo_Platform = table.Column<int>(type: "integer", nullable: false),
                    Info_AvatarUrl = table.Column<string>(type: "text", nullable: false),
                    Info_Country = table.Column<string>(type: "text", nullable: false),
                    Info_CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Info_Username = table.Column<string>(type: "text", nullable: false),
                    LinkedAccounts_BeatLeaderId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LinkedAccounts_DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    LinkedAccounts_ScoreSaberId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    SubscriptionInfo_Tier = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayModes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayModes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Songs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hash = table.Column<string>(type: "character(40)", fixedLength: true, maxLength: 40, nullable: false),
                    BeatSaverKey = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Info_BeatSaverName = table.Column<string>(type: "text", nullable: false),
                    Info_MapperName = table.Column<string>(type: "text", nullable: false),
                    Info_SongAuthorName = table.Column<string>(type: "text", nullable: false),
                    Info_SongName = table.Column<string>(type: "text", nullable: false),
                    Info_SongSubName = table.Column<string>(type: "text", nullable: false),
                    Stats_BPM = table.Column<float>(type: "real", nullable: false),
                    Stats_DurationSec = table.Column<float>(type: "real", nullable: false),
                    Stats_IsAutoMapped = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.Id);
                    table.UniqueConstraint("AK_Songs_Hash", x => x.Hash);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    Info_Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Info_Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Info_Description = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Info_Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contexts_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Points",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    CurveSettings_Accuracy = table.Column<NpgsqlPoint[]>(type: "point[]", nullable: false),
                    CurveSettings_Difficulty = table.Column<NpgsqlPoint[]>(type: "point[]", nullable: false),
                    Info_Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Info_Name = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    ModifierValues_BatteryEnergy = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_DisappearingArrows = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_FasterSong = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_GhostNotes = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_InstaFail = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_NoArrows = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_NoBombs = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_NoFail = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_NoObstacles = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_OffPlatform = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_OldDots = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_ProMode = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_SlowerSong = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_SmallNotes = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_StrictAngles = table.Column<float>(type: "real", nullable: false),
                    ModifierValues_SuperFastSong = table.Column<float>(type: "real", nullable: false),
                    WeightingSettings_IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WeightingSettings_Multiplier = table.Column<double>(type: "double precision", nullable: false),
                    WeightingSettings_TopScoreCountToConsider = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Points", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Points_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Boosts",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boosts", x => new { x.GuildId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_Boosts_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Boosts_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Permissions = table.Column<int>(type: "integer", nullable: false),
                    JoinState = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => new { x.GuildId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_Members_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Members_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BrowserVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Sessions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SongDifficulties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BLLeaderboardId = table.Column<string>(type: "text", nullable: true),
                    SSLeaderboardId = table.Column<int>(type: "integer", nullable: true),
                    GameModeId = table.Column<int>(type: "integer", nullable: false),
                    SongId = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    Stats_BombCount = table.Column<int>(type: "integer", nullable: false),
                    Stats_Duration = table.Column<double>(type: "double precision", nullable: false),
                    Stats_MaxScore = table.Column<int>(type: "integer", nullable: false),
                    Stats_NoteCount = table.Column<int>(type: "integer", nullable: false),
                    Stats_NoteJumpSpeed = table.Column<float>(type: "real", nullable: false),
                    Stats_NotesPerSecond = table.Column<float>(type: "real", nullable: false),
                    Stats_ObstacleCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongDifficulties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongDifficulties_GameModes_GameModeId",
                        column: x => x.GameModeId,
                        principalTable: "GameModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SongDifficulties_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    IsLocking = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Info_Color = table.Column<int>(type: "integer", nullable: false),
                    Info_Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MinStar = table.Column<float>(type: "real", nullable: true),
                    RequiredPassCount = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Levels_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Levels_Contexts_ContextId",
                        column: x => x.ContextId,
                        principalTable: "Contexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Levels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RankedMaps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    Info_CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Info_EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Rating_AccStar = table.Column<float>(type: "real", nullable: false),
                    Rating_DiffStar = table.Column<float>(type: "real", nullable: false),
                    Requirements_MandatoryModifiers = table.Column<int>(type: "integer", nullable: false),
                    Requirements_MaxPauseDurationSec = table.Column<float>(type: "real", nullable: true),
                    Requirements_MinAccuracy = table.Column<float>(type: "real", nullable: true),
                    Requirements_NeedConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    Requirements_NeedFullCombo = table.Column<bool>(type: "boolean", nullable: false),
                    Requirements_ProhibitedModifiers = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankedMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankedMaps_Contexts_ContextId",
                        column: x => x.ContextId,
                        principalTable: "Contexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedMaps_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContextPoint",
                columns: table => new
                {
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    PointsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextPoint", x => new { x.ContextId, x.PointsId });
                    table.ForeignKey(
                        name: "FK_ContextPoint_Contexts_ContextId",
                        column: x => x.ContextId,
                        principalTable: "Contexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContextPoint_Points_PointsId",
                        column: x => x.PointsId,
                        principalTable: "Points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContextMembers",
                columns: table => new
                {
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextMembers", x => new { x.GuildId, x.ContextId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_ContextMembers_Contexts_ContextId",
                        column: x => x.ContextId,
                        principalTable: "Contexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContextMembers_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContextMembers_Members_GuildId_PlayerId",
                        columns: x => new { x.GuildId, x.PlayerId },
                        principalTable: "Members",
                        principalColumns: new[] { "GuildId", "PlayerId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SongDifficultyId = table.Column<long>(type: "bigint", nullable: false),
                    BaseScore = table.Column<int>(type: "integer", nullable: false),
                    Modifiers = table.Column<int>(type: "integer", nullable: false),
                    SetAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MaxCombo = table.Column<int>(type: "integer", nullable: true),
                    IsFullCombo = table.Column<bool>(type: "boolean", nullable: false),
                    MissedNotes = table.Column<int>(type: "integer", nullable: false),
                    BadCuts = table.Column<int>(type: "integer", nullable: false),
                    HMD = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    BeatLeaderScoreId = table.Column<int>(type: "integer", nullable: true),
                    Statistics_Discriminator = table.Column<string>(type: "text", nullable: true),
                    AccuracyTracker_AccLeft = table.Column<float>(type: "real", nullable: true),
                    AccuracyTracker_AccRight = table.Column<float>(type: "real", nullable: true),
                    AccuracyTracker_AccuracyGrid = table.Column<float[]>(type: "real[]", nullable: true),
                    AccuracyTracker_LeftAverageCutGraphGrid = table.Column<float[]>(type: "real[]", nullable: true),
                    AccuracyTracker_LeftPostSwing = table.Column<float>(type: "real", nullable: true),
                    AccuracyTracker_LeftPreSwing = table.Column<float>(type: "real", nullable: true),
                    AccuracyTracker_LeftTimeDependence = table.Column<float>(type: "real", nullable: true),
                    AccuracyTracker_RightAverageCutGraphGrid = table.Column<float[]>(type: "real[]", nullable: true),
                    AccuracyTracker_RightPostSwing = table.Column<float>(type: "real", nullable: true),
                    AccuracyTracker_RightPreSwing = table.Column<float>(type: "real", nullable: true),
                    AccuracyTracker_RightTimeDependence = table.Column<float>(type: "real", nullable: true),
                    HitTracker_LeftBadCuts = table.Column<int>(type: "integer", nullable: true),
                    HitTracker_LeftBombs = table.Column<int>(type: "integer", nullable: true),
                    HitTracker_LeftMiss = table.Column<int>(type: "integer", nullable: true),
                    HitTracker_LeftTiming = table.Column<float>(type: "real", nullable: true),
                    HitTracker_Max115Streak = table.Column<int>(type: "integer", nullable: true),
                    HitTracker_RightBadCuts = table.Column<int>(type: "integer", nullable: true),
                    HitTracker_RightBombs = table.Column<int>(type: "integer", nullable: true),
                    HitTracker_RightMiss = table.Column<int>(type: "integer", nullable: true),
                    HitTracker_RightTiming = table.Column<float>(type: "real", nullable: true),
                    ScoreGraphTracker_Graph = table.Column<List<float>>(type: "real[]", nullable: true),
                    WinTracker_AverageHeight = table.Column<float>(type: "real", nullable: true),
                    WinTracker_EndTime = table.Column<float>(type: "real", nullable: true),
                    WinTracker_IsWin = table.Column<bool>(type: "boolean", nullable: true),
                    WinTracker_JumpDistance = table.Column<float>(type: "real", nullable: true),
                    WinTracker_MaxScore = table.Column<int>(type: "integer", nullable: true),
                    WinTracker_PauseCount = table.Column<int>(type: "integer", nullable: true),
                    WinTracker_TotalPauseDuration = table.Column<float>(type: "real", nullable: true),
                    WinTracker_TotalScore = table.Column<int>(type: "integer", nullable: true),
                    AverageHeadPosition_X = table.Column<float>(type: "real", nullable: true),
                    AverageHeadPosition_Y = table.Column<float>(type: "real", nullable: true),
                    AverageHeadPosition_Z = table.Column<float>(type: "real", nullable: true),
                    ScoreSaberScoreId = table.Column<int>(type: "integer", nullable: true),
                    DeviceHmd = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeviceControllerLeft = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeviceControllerRight = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scores_SongDifficulties_SongDifficultyId",
                        column: x => x.SongDifficultyId,
                        principalTable: "SongDifficulties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryRankedMap",
                columns: table => new
                {
                    CategoriesId = table.Column<int>(type: "integer", nullable: false),
                    RankedMapId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryRankedMap", x => new { x.CategoriesId, x.RankedMapId });
                    table.ForeignKey(
                        name: "FK_CategoryRankedMap_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryRankedMap_RankedMaps_RankedMapId",
                        column: x => x.RankedMapId,
                        principalTable: "RankedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapVersions",
                columns: table => new
                {
                    RankedMapId = table.Column<long>(type: "bigint", nullable: false),
                    SongDifficultyId = table.Column<long>(type: "bigint", nullable: false),
                    PlayModeId = table.Column<int>(type: "integer", nullable: false),
                    SongId = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Order = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapVersions", x => new { x.RankedMapId, x.SongDifficultyId, x.PlayModeId });
                    table.ForeignKey(
                        name: "FK_MapVersions_PlayModes_PlayModeId",
                        column: x => x.PlayModeId,
                        principalTable: "PlayModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MapVersions_RankedMaps_RankedMapId",
                        column: x => x.RankedMapId,
                        principalTable: "RankedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MapVersions_SongDifficulties_SongDifficultyId",
                        column: x => x.SongDifficultyId,
                        principalTable: "SongDifficulties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MapVersions_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RankedMapRankedMapListLevel",
                columns: table => new
                {
                    LevelsId = table.Column<int>(type: "integer", nullable: false),
                    RankedMapsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankedMapRankedMapListLevel", x => new { x.LevelsId, x.RankedMapsId });
                    table.ForeignKey(
                        name: "FK_RankedMapRankedMapListLevel_Levels_LevelsId",
                        column: x => x.LevelsId,
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedMapRankedMapListLevel_RankedMaps_RankedMapsId",
                        column: x => x.RankedMapsId,
                        principalTable: "RankedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberLevelStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    LevelId = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    PassCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberLevelStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberLevelStats_ContextMembers_GuildId_ContextId_PlayerId",
                        columns: x => new { x.GuildId, x.ContextId, x.PlayerId },
                        principalTable: "ContextMembers",
                        principalColumns: new[] { "GuildId", "ContextId", "PlayerId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberLevelStats_Levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberPointStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    PointId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    Points = table.Column<float>(type: "real", nullable: false),
                    Xp = table.Column<float>(type: "real", nullable: false),
                    PassCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberPointStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberPointStats_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberPointStats_ContextMembers_GuildId_ContextId_PlayerId",
                        columns: x => new { x.GuildId, x.ContextId, x.PlayerId },
                        principalTable: "ContextMembers",
                        principalColumns: new[] { "GuildId", "ContextId", "PlayerId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberPointStats_Points_PointId",
                        column: x => x.PointId,
                        principalTable: "Points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RankedScores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    RankedMapId = table.Column<long>(type: "bigint", nullable: false),
                    SongDifficultyId = table.Column<long>(type: "bigint", nullable: false),
                    PointId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ScoreId = table.Column<int>(type: "integer", nullable: false),
                    PrevScoreId = table.Column<int>(type: "integer", nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false),
                    DenyReason = table.Column<int>(type: "integer", nullable: false),
                    EffectiveScore = table.Column<int>(type: "integer", nullable: false),
                    RawPoints = table.Column<float>(type: "real", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankedScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankedScores_Contexts_ContextId",
                        column: x => x.ContextId,
                        principalTable: "Contexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedScores_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedScores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedScores_Points_PointId",
                        column: x => x.PointId,
                        principalTable: "Points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedScores_RankedMaps_RankedMapId",
                        column: x => x.RankedMapId,
                        principalTable: "RankedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedScores_Scores_PrevScoreId",
                        column: x => x.PrevScoreId,
                        principalTable: "Scores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedScores_Scores_ScoreId",
                        column: x => x.ScoreId,
                        principalTable: "Scores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedScores_SongDifficulties_SongDifficultyId",
                        column: x => x.SongDifficultyId,
                        principalTable: "SongDifficulties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boosts_PlayerId",
                table: "Boosts",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_GuildId",
                table: "Categories",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRankedMap_RankedMapId",
                table: "CategoryRankedMap",
                column: "RankedMapId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextMembers_ContextId",
                table: "ContextMembers",
                column: "ContextId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextMembers_GuildId_PlayerId",
                table: "ContextMembers",
                columns: new[] { "GuildId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContextPoint_PointsId",
                table: "ContextPoint",
                column: "PointsId");

            migrationBuilder.CreateIndex(
                name: "IX_Contexts_GuildId",
                table: "Contexts",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Levels_CategoryId",
                table: "Levels",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Levels_ContextId",
                table: "Levels",
                column: "ContextId");

            migrationBuilder.CreateIndex(
                name: "IX_Levels_GuildId_ContextId_CategoryId",
                table: "Levels",
                columns: new[] { "GuildId", "ContextId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MapVersions_PlayModeId",
                table: "MapVersions",
                column: "PlayModeId");

            migrationBuilder.CreateIndex(
                name: "IX_MapVersions_SongDifficultyId",
                table: "MapVersions",
                column: "SongDifficultyId");

            migrationBuilder.CreateIndex(
                name: "IX_MapVersions_SongId",
                table: "MapVersions",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLevelStats_ContextId_PlayerId",
                table: "MemberLevelStats",
                columns: new[] { "ContextId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberLevelStats_GuildId_ContextId_PlayerId_LevelId",
                table: "MemberLevelStats",
                columns: new[] { "GuildId", "ContextId", "PlayerId", "LevelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberLevelStats_LevelId",
                table: "MemberLevelStats",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPointStats_CategoryId",
                table: "MemberPointStats",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberPointStats_ContextId_PointId_CategoryId",
                table: "MemberPointStats",
                columns: new[] { "ContextId", "PointId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberPointStats_GuildId_ContextId_PlayerId_PointId_Categor~",
                table: "MemberPointStats",
                columns: new[] { "GuildId", "ContextId", "PlayerId", "PointId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberPointStats_PointId",
                table: "MemberPointStats",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_PlayerId",
                table: "Members",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Points_GuildId",
                table: "Points",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedMapRankedMapListLevel_RankedMapsId",
                table: "RankedMapRankedMapListLevel",
                column: "RankedMapsId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedMaps_ContextId",
                table: "RankedMaps",
                column: "ContextId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedMaps_GuildId",
                table: "RankedMaps",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_ContextId_PointId_RankedMapId",
                table: "RankedScores",
                columns: new[] { "ContextId", "PointId", "RankedMapId" });

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_GuildId",
                table: "RankedScores",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_PlayerId",
                table: "RankedScores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_PointId",
                table: "RankedScores",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_PrevScoreId",
                table: "RankedScores",
                column: "PrevScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_RankedMapId",
                table: "RankedScores",
                column: "RankedMapId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_ScoreId",
                table: "RankedScores",
                column: "ScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_SongDifficultyId",
                table: "RankedScores",
                column: "SongDifficultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_PlayerId",
                table: "Scores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_SongDifficultyId",
                table: "Scores",
                column: "SongDifficultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_PlayerId",
                table: "Sessions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SongDifficulties_GameModeId",
                table: "SongDifficulties",
                column: "GameModeId");

            migrationBuilder.CreateIndex(
                name: "IX_SongDifficulties_SongId_GameModeId_Difficulty",
                table: "SongDifficulties",
                columns: new[] { "SongId", "GameModeId", "Difficulty" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Boosts");

            migrationBuilder.DropTable(
                name: "CategoryRankedMap");

            migrationBuilder.DropTable(
                name: "ContextPoint");

            migrationBuilder.DropTable(
                name: "MapVersions");

            migrationBuilder.DropTable(
                name: "MemberLevelStats");

            migrationBuilder.DropTable(
                name: "MemberPointStats");

            migrationBuilder.DropTable(
                name: "RankedMapRankedMapListLevel");

            migrationBuilder.DropTable(
                name: "RankedScores");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "PlayModes");

            migrationBuilder.DropTable(
                name: "ContextMembers");

            migrationBuilder.DropTable(
                name: "Levels");

            migrationBuilder.DropTable(
                name: "Points");

            migrationBuilder.DropTable(
                name: "RankedMaps");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Contexts");

            migrationBuilder.DropTable(
                name: "SongDifficulties");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "GameModes");

            migrationBuilder.DropTable(
                name: "Songs");
        }
    }
}
