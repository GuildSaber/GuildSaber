using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations;

/// <inheritdoc />
public partial class _20261027081814_Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "ticker");

        migrationBuilder.CreateTable(
            name: "Controllers",
            columns: table => new
            {
                Id = table.Column<short>(type: "smallint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                BeatLeaderControllerId = table.Column<int>(type: "integer", nullable: true),
                BeatLeaderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                ScoreSaberKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Controllers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CronTickers",
            schema: "ticker",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Expression = table.Column<string>(type: "text", nullable: true),
                Request = table.Column<byte[]>(type: "bytea", nullable: true),
                Retries = table.Column<int>(type: "integer", nullable: false),
                RetryIntervals = table.Column<int[]>(type: "integer[]", nullable: true),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                IsSystemPaused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                Function = table.Column<string>(type: "text", nullable: true),
                Description = table.Column<string>(type: "text", nullable: true),
                InitIdentifier = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CronTickers", x => x.Id);
            });

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
                DiscordInfo_ConfirmedScoreFeedChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                DiscordInfo_DiscordInviteCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                DiscordInfo_MainDiscordGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                DiscordInfo_RefusedScoreFeedChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                Info_Color = table.Column<int>(type: "integer", nullable: false),
                Info_CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Info_Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Info_Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Info_SmallName = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                Requirements_AccountAgeUnix = table.Column<int>(type: "integer", nullable: true),
                Requirements_MaxPP = table.Column<int>(type: "integer", nullable: true),
                Requirements_MaxRank = table.Column<int>(type: "integer", nullable: true),
                Requirements_MinPP = table.Column<int>(type: "integer", nullable: true),
                Requirements_MinPlayCount = table.Column<int>(type: "integer", nullable: true),
                Requirements_MinRank = table.Column<int>(type: "integer", nullable: true),
                Requirements_RequireSubmission = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Guilds", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Hmds",
            columns: table => new
            {
                Id = table.Column<short>(type: "smallint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                BeatLeaderHmdId = table.Column<int>(type: "integer", nullable: true),
                BeatLeaderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                ScoreSaberKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Hmds", x => x.Id);
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
            name: "TimeTickers",
            schema: "ticker",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Function = table.Column<string>(type: "text", nullable: true),
                Description = table.Column<string>(type: "text", nullable: true),
                InitIdentifier = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                LockHolder = table.Column<string>(type: "text", nullable: true),
                Request = table.Column<byte[]>(type: "bytea", nullable: true),
                ExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExceptionMessage = table.Column<string>(type: "text", nullable: true),
                SkippedReason = table.Column<string>(type: "text", nullable: true),
                ElapsedTime = table.Column<long>(type: "bigint", nullable: false),
                Retries = table.Column<int>(type: "integer", nullable: false),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                RetryIntervals = table.Column<int[]>(type: "integer[]", nullable: true),
                ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                RunCondition = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TimeTickers", x => x.Id);
                table.ForeignKey(
                    name: "FK_TimeTickers_TimeTickers_ParentId",
                    column: x => x.ParentId,
                    principalSchema: "ticker",
                    principalTable: "TimeTickers",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "CronTickerOccurrences",
            schema: "ticker",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                LockHolder = table.Column<string>(type: "text", nullable: true),
                ExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CronTickerId = table.Column<Guid>(type: "uuid", nullable: false),
                LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ExceptionMessage = table.Column<string>(type: "text", nullable: true),
                SkippedReason = table.Column<string>(type: "text", nullable: true),
                ElapsedTime = table.Column<long>(type: "bigint", nullable: false),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CronTickerOccurrences", x => x.Id);
                table.ForeignKey(
                    name: "FK_CronTickerOccurrences_CronTickers_CronTickerId",
                    column: x => x.CronTickerId,
                    principalSchema: "ticker",
                    principalTable: "CronTickers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
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
            name: "Players",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Platform = table.Column<int>(type: "integer", nullable: false),
                HmdId = table.Column<short>(type: "smallint", nullable: true),
                IsManager = table.Column<bool>(type: "boolean", nullable: false),
                Info_AvatarUrl = table.Column<string>(type: "text", nullable: false),
                Info_Country = table.Column<string>(type: "text", nullable: false),
                Info_CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Info_Username = table.Column<string>(type: "text", nullable: false),
                LinkedAccounts_BLNativeId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                LinkedAccounts_DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                LinkedAccounts_MetaPCId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                LinkedAccounts_ScoreSaberId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                LinkedAccounts_SteamId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                SubscriptionInfo_Tier = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Players", x => x.Id);
                table.ForeignKey(
                    name: "FK_Players_Hmds_HmdId",
                    column: x => x.HmdId,
                    principalTable: "Hmds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
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
            name: "Achievements",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                GuildId = table.Column<int>(type: "integer", nullable: false),
                ContextId = table.Column<int>(type: "integer", nullable: false),
                CategoryId = table.Column<int>(type: "integer", nullable: true),
                ProgressionOrder = table.Column<long>(type: "bigint", nullable: true),
                IsLocking = table.Column<bool>(type: "boolean", nullable: false),
                UnlockXp = table.Column<float>(type: "real", nullable: false),
                type = table.Column<byte>(type: "smallint", nullable: false),
                DiscordBindings_RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                Info_Color = table.Column<int>(type: "integer", nullable: false),
                Info_Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                MinStar = table.Column<float>(type: "real", nullable: true),
                MaxStar = table.Column<float>(type: "real", nullable: true),
                RequiredPassCount = table.Column<long>(type: "bigint", nullable: true),
                RankedMapListAchievement_RequiredPassCount = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Achievements", x => x.Id);
                table.CheckConstraint("CK_Achievements_StarRange", "\"MaxStar\" IS NULL OR \"MinStar\" < \"MaxStar\"");
                table.CheckConstraint("CK_Achievements_UnlockXp_Range", "\"UnlockXp\" BETWEEN 0 AND 3.4028235E+38");
                table.ForeignKey(
                    name: "FK_Achievements_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Achievements_Contexts_ContextId",
                    column: x => x.ContextId,
                    principalTable: "Contexts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Achievements_Guilds_GuildId",
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
            name: "MapRequestThreads",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ContextId = table.Column<int>(type: "integer", nullable: false),
                AuthorId = table.Column<int>(type: "integer", nullable: true),
                IsLegacyImport = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapRequestThreads", x => x.Id);
                table.ForeignKey(
                    name: "FK_MapRequestThreads_Contexts_ContextId",
                    column: x => x.ContextId,
                    principalTable: "Contexts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MapRequestThreads_Players_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
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
                HmdId = table.Column<short>(type: "smallint", nullable: true),
                LeftControllerId = table.Column<short>(type: "smallint", nullable: true),
                RightControllerId = table.Column<short>(type: "smallint", nullable: true),
                Type = table.Column<byte>(type: "smallint", nullable: false),
                Statistics_Exists = table.Column<bool>(type: "boolean", nullable: true),
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
                WinTracker_TotalScore = table.Column<int>(type: "integer", nullable: true),
                AverageHeadPosition_X = table.Column<float>(type: "real", nullable: true),
                AverageHeadPosition_Y = table.Column<float>(type: "real", nullable: true),
                AverageHeadPosition_Z = table.Column<float>(type: "real", nullable: true),
                Statistics_WinTracker_PauseStatistics_Exists = table.Column<bool>(type: "boolean", nullable: true),
                PauseStatistics_PauseCount = table.Column<int>(type: "integer", nullable: true),
                PauseStatistics_TotalPauseDuration = table.Column<float>(type: "real", nullable: true),
                BeatLeaderScoreId = table.Column<int>(type: "integer", nullable: true),
                ScoreSaberScoreId = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Scores", x => x.Id);
                table.ForeignKey(
                    name: "FK_Scores_Controllers_LeftControllerId",
                    column: x => x.LeftControllerId,
                    principalTable: "Controllers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Scores_Controllers_RightControllerId",
                    column: x => x.RightControllerId,
                    principalTable: "Controllers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Scores_Hmds_HmdId",
                    column: x => x.HmdId,
                    principalTable: "Hmds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
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
            name: "MapProposals",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ThreadId = table.Column<long>(type: "bigint", nullable: false),
                AuthorId = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Comment = table.Column<string>(type: "text", nullable: true),
                status = table.Column<byte>(type: "smallint", nullable: false),
                Rating_AccStar = table.Column<float>(type: "real", nullable: true),
                Rating_DiffStar = table.Column<float>(type: "real", nullable: true),
                Rating_IsAccStarAutoCalculated = table.Column<bool>(type: "boolean", nullable: false),
                Rating_IsDiffStarAutoCalculated = table.Column<bool>(type: "boolean", nullable: false),
                Requirements_MandatoryModifiers = table.Column<int>(type: "integer", nullable: false),
                Requirements_MaxPauseDurationSec = table.Column<float>(type: "real", nullable: true),
                Requirements_MinAccuracy = table.Column<float>(type: "real", nullable: true),
                Requirements_NeedConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                Requirements_NeedFullCombo = table.Column<bool>(type: "boolean", nullable: false),
                Requirements_ProhibitedModifiers = table.Column<int>(type: "integer", nullable: false),
                SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AcceptedById = table.Column<int>(type: "integer", nullable: true),
                ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ClosedById = table.Column<int>(type: "integer", nullable: true),
                Reason = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapProposals", x => x.Id);
                table.CheckConstraint("CK_MapProposals_Lifecycle", "(\"status\" = 0\n    AND \"SubmittedAt\" IS NULL AND \"AcceptedAt\" IS NULL AND \"ClosedAt\" IS NULL)\nOR (\"status\" = 1\n    AND \"SubmittedAt\" IS NOT NULL AND \"AcceptedAt\" IS NULL AND \"ClosedAt\" IS NULL)\nOR (\"status\" = 2\n    AND \"SubmittedAt\" IS NOT NULL AND \"AcceptedAt\" IS NOT NULL AND \"ClosedAt\" IS NULL)\nOR (\"status\" = 3\n    AND \"AcceptedAt\" IS NULL AND \"ClosedAt\" IS NOT NULL)");
                table.CheckConstraint("CK_MapProposals_ResolvedRating", "\"SubmittedAt\" IS NULL\nOR (\"Rating_AccStar\" IS NOT NULL\n    AND \"Rating_DiffStar\" IS NOT NULL)");
                table.ForeignKey(
                    name: "FK_MapProposals_MapRequestThreads_ThreadId",
                    column: x => x.ThreadId,
                    principalTable: "MapRequestThreads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MapProposals_Players_AcceptedById",
                    column: x => x.AcceptedById,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_MapProposals_Players_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_MapProposals_Players_ClosedById",
                    column: x => x.ClosedById,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "MapRequestThreadBoosts",
            columns: table => new
            {
                PlayerId = table.Column<int>(type: "integer", nullable: false),
                ThreadId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapRequestThreadBoosts", x => new { x.PlayerId, x.ThreadId });
                table.ForeignKey(
                    name: "FK_MapRequestThreadBoosts_MapRequestThreads_ThreadId",
                    column: x => x.ThreadId,
                    principalTable: "MapRequestThreads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MapRequestThreadBoosts_Players_PlayerId",
                    column: x => x.PlayerId,
                    principalTable: "Players",
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
                ThreadId = table.Column<long>(type: "bigint", nullable: false),
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
                table.ForeignKey(
                    name: "FK_RankedMaps_MapRequestThreads_ThreadId",
                    column: x => x.ThreadId,
                    principalTable: "MapRequestThreads",
                    principalColumn: "Id");
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
            name: "MapProposalAchievements",
            columns: table => new
            {
                ProposalId = table.Column<long>(type: "bigint", nullable: false),
                AchievementId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapProposalAchievements", x => new { x.ProposalId, x.AchievementId });
                table.ForeignKey(
                    name: "FK_MapProposalAchievements_MapProposals_ProposalId",
                    column: x => x.ProposalId,
                    principalTable: "MapProposals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MapProposalCategories",
            columns: table => new
            {
                ProposalId = table.Column<long>(type: "bigint", nullable: false),
                CategoryId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapProposalCategories", x => new { x.ProposalId, x.CategoryId });
                table.ForeignKey(
                    name: "FK_MapProposalCategories_MapProposals_ProposalId",
                    column: x => x.ProposalId,
                    principalTable: "MapProposals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MapProposalVersions",
            columns: table => new
            {
                ProposalId = table.Column<long>(type: "bigint", nullable: false),
                SongDifficultyId = table.Column<long>(type: "bigint", nullable: false),
                PlayModeId = table.Column<int>(type: "integer", nullable: false),
                PlayModeName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Order = table.Column<byte>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapProposalVersions", x => new { x.ProposalId, x.SongDifficultyId, x.PlayModeId });
                table.ForeignKey(
                    name: "FK_MapProposalVersions_MapProposals_ProposalId",
                    column: x => x.ProposalId,
                    principalTable: "MapProposals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MapProposalVersions_SongDifficulties_SongDifficultyId",
                    column: x => x.SongDifficultyId,
                    principalTable: "SongDifficulties",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "MapProposalVotes",
            columns: table => new
            {
                ProposalId = table.Column<long>(type: "bigint", nullable: false),
                PlayerId = table.Column<int>(type: "integer", nullable: false),
                Kind = table.Column<byte>(type: "smallint", nullable: false),
                IsPositive = table.Column<bool>(type: "boolean", nullable: false),
                VotedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Reason = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapProposalVotes", x => new { x.ProposalId, x.PlayerId, x.Kind });
                table.CheckConstraint("CK_MapProposalVotes_Kind", "(\"Kind\" = 0 AND \"Reason\" IS NULL) OR \"Kind\" = 1");
                table.ForeignKey(
                    name: "FK_MapProposalVotes_MapProposals_ProposalId",
                    column: x => x.ProposalId,
                    principalTable: "MapProposals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MapProposalVotes_Players_PlayerId",
                    column: x => x.PlayerId,
                    principalTable: "Players",
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
            name: "RankedMapRankedMapListAchievement",
            columns: table => new
            {
                AchievementsId = table.Column<int>(type: "integer", nullable: false),
                RankedMapsId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RankedMapRankedMapListAchievement", x => new { x.AchievementsId, x.RankedMapsId });
                table.ForeignKey(
                    name: "FK_RankedMapRankedMapListAchievement_Achievements_Achievements~",
                    column: x => x.AchievementsId,
                    principalTable: "Achievements",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RankedMapRankedMapListAchievement_RankedMaps_RankedMapsId",
                    column: x => x.RankedMapsId,
                    principalTable: "RankedMaps",
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
                IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                EffectiveScore = table.Column<int>(type: "integer", nullable: false),
                EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Type = table.Column<byte>(type: "smallint", nullable: false),
                InvalidReason = table.Column<int>(type: "integer", nullable: true),
                RawPoints = table.Column<float>(type: "real", nullable: true),
                Rank = table.Column<int>(type: "integer", nullable: true)
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

        migrationBuilder.CreateTable(
            name: "MemberAchievementStats",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                GuildId = table.Column<int>(type: "integer", nullable: false),
                ContextId = table.Column<int>(type: "integer", nullable: false),
                PlayerId = table.Column<int>(type: "integer", nullable: false),
                AchievementId = table.Column<int>(type: "integer", nullable: false),
                IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                PassCount = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemberAchievementStats", x => x.Id);
                table.ForeignKey(
                    name: "FK_MemberAchievementStats_Achievements_AchievementId",
                    column: x => x.AchievementId,
                    principalTable: "Achievements",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MemberAchievementStats_ContextMembers_GuildId_ContextId_Pla~",
                    columns: x => new { x.GuildId, x.ContextId, x.PlayerId },
                    principalTable: "ContextMembers",
                    principalColumns: new[] { "GuildId", "ContextId", "PlayerId" },
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
                    name: "FK_MemberPointStats_Contexts_ContextId",
                    column: x => x.ContextId,
                    principalTable: "Contexts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MemberPointStats_Guilds_GuildId",
                    column: x => x.GuildId,
                    principalTable: "Guilds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MemberPointStats_Players_PlayerId",
                    column: x => x.PlayerId,
                    principalTable: "Players",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MemberPointStats_Points_PointId",
                    column: x => x.PointId,
                    principalTable: "Points",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Achievements_CategoryId",
            table: "Achievements",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Achievements_ContextId",
            table: "Achievements",
            column: "ContextId");

        migrationBuilder.CreateIndex(
            name: "IX_Achievements_GuildId_ContextId_CategoryId",
            table: "Achievements",
            columns: new[] { "GuildId", "ContextId", "CategoryId" });

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
            name: "IX_Controllers_BeatLeaderControllerId",
            table: "Controllers",
            column: "BeatLeaderControllerId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Controllers_BeatLeaderKey",
            table: "Controllers",
            column: "BeatLeaderKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Controllers_ScoreSaberKey",
            table: "Controllers",
            column: "ScoreSaberKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CronTickerOccurrence_CronTickerId",
            schema: "ticker",
            table: "CronTickerOccurrences",
            column: "CronTickerId");

        migrationBuilder.CreateIndex(
            name: "IX_CronTickerOccurrence_ExecutionTime",
            schema: "ticker",
            table: "CronTickerOccurrences",
            column: "ExecutionTime");

        migrationBuilder.CreateIndex(
            name: "IX_CronTickerOccurrence_Status_ExecutionTime",
            schema: "ticker",
            table: "CronTickerOccurrences",
            columns: new[] { "Status", "ExecutionTime" });

        migrationBuilder.CreateIndex(
            name: "UQ_CronTickerId_ExecutionTime",
            schema: "ticker",
            table: "CronTickerOccurrences",
            columns: new[] { "CronTickerId", "ExecutionTime" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CronTickers_Expression",
            schema: "ticker",
            table: "CronTickers",
            column: "Expression");

        migrationBuilder.CreateIndex(
            name: "IX_Function_Expression",
            schema: "ticker",
            table: "CronTickers",
            columns: new[] { "Function", "Expression" });

        migrationBuilder.CreateIndex(
            name: "IX_Hmds_BeatLeaderHmdId",
            table: "Hmds",
            column: "BeatLeaderHmdId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Hmds_BeatLeaderKey",
            table: "Hmds",
            column: "BeatLeaderKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Hmds_ScoreSaberKey",
            table: "Hmds",
            column: "ScoreSaberKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MapProposals_AcceptedById",
            table: "MapProposals",
            column: "AcceptedById");

        migrationBuilder.CreateIndex(
            name: "IX_MapProposals_AuthorId",
            table: "MapProposals",
            column: "AuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_MapProposals_ClosedById",
            table: "MapProposals",
            column: "ClosedById");

        migrationBuilder.CreateIndex(
            name: "IX_MapProposals_ThreadId",
            table: "MapProposals",
            column: "ThreadId");

        migrationBuilder.CreateIndex(
            name: "IX_MapProposalVersions_SongDifficultyId",
            table: "MapProposalVersions",
            column: "SongDifficultyId");

        migrationBuilder.CreateIndex(
            name: "IX_MapProposalVotes_PlayerId",
            table: "MapProposalVotes",
            column: "PlayerId");

        migrationBuilder.CreateIndex(
            name: "IX_MapRequestThreadBoosts_ThreadId",
            table: "MapRequestThreadBoosts",
            column: "ThreadId");

        migrationBuilder.CreateIndex(
            name: "IX_MapRequestThreads_AuthorId",
            table: "MapRequestThreads",
            column: "AuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_MapRequestThreads_ContextId",
            table: "MapRequestThreads",
            column: "ContextId");

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
            name: "IX_MemberAchievementStats_AchievementId",
            table: "MemberAchievementStats",
            column: "AchievementId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberAchievementStats_ContextId_PlayerId",
            table: "MemberAchievementStats",
            columns: new[] { "ContextId", "PlayerId" });

        migrationBuilder.CreateIndex(
            name: "IX_MemberAchievementStats_GuildId_ContextId_PlayerId_Achieveme~",
            table: "MemberAchievementStats",
            columns: new[] { "GuildId", "ContextId", "PlayerId", "AchievementId" },
            unique: true);

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
            name: "IX_MemberPointStats_PlayerId",
            table: "MemberPointStats",
            column: "PlayerId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberPointStats_PointId",
            table: "MemberPointStats",
            column: "PointId");

        migrationBuilder.CreateIndex(
            name: "IX_Members_PlayerId",
            table: "Members",
            column: "PlayerId");

        migrationBuilder.CreateIndex(
            name: "IX_Players_HmdId",
            table: "Players",
            column: "HmdId");

        migrationBuilder.CreateIndex(
            name: "IX_Players_LinkedAccounts_BLNativeId",
            table: "Players",
            column: "LinkedAccounts_BLNativeId");

        migrationBuilder.CreateIndex(
            name: "IX_Players_LinkedAccounts_DiscordId",
            table: "Players",
            column: "LinkedAccounts_DiscordId");

        migrationBuilder.CreateIndex(
            name: "IX_Players_LinkedAccounts_MetaPCId",
            table: "Players",
            column: "LinkedAccounts_MetaPCId");

        migrationBuilder.CreateIndex(
            name: "IX_Players_LinkedAccounts_ScoreSaberId",
            table: "Players",
            column: "LinkedAccounts_ScoreSaberId");

        migrationBuilder.CreateIndex(
            name: "IX_Players_LinkedAccounts_SteamId",
            table: "Players",
            column: "LinkedAccounts_SteamId");

        migrationBuilder.CreateIndex(
            name: "IX_Points_GuildId",
            table: "Points",
            column: "GuildId");

        migrationBuilder.CreateIndex(
            name: "IX_RankedMapRankedMapListAchievement_RankedMapsId",
            table: "RankedMapRankedMapListAchievement",
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
            name: "IX_RankedMaps_ThreadId",
            table: "RankedMaps",
            column: "ThreadId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_ContextId_PlayerId_RawPoints_Id",
            table: "RankedScores",
            columns: new[] { "ContextId", "PlayerId", "RawPoints", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_ContextId_PointId_RankedMapId",
            table: "RankedScores",
            columns: new[] { "ContextId", "PointId", "RankedMapId" });

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_EditedAt",
            table: "RankedScores",
            column: "EditedAt");

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_GuildId",
            table: "RankedScores",
            column: "GuildId");

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_IsSelected",
            table: "RankedScores",
            column: "IsSelected");

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_PlayerId_IsSelected",
            table: "RankedScores",
            columns: new[] { "PlayerId", "IsSelected" });

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_PointId",
            table: "RankedScores",
            column: "PointId");

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_PrevScoreId",
            table: "RankedScores",
            column: "PrevScoreId");

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_RankedMapId_PlayerId_IsSelected",
            table: "RankedScores",
            columns: new[] { "RankedMapId", "PlayerId", "IsSelected" });

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_ScoreId",
            table: "RankedScores",
            column: "ScoreId");

        migrationBuilder.CreateIndex(
            name: "IX_RankedScores_SongDifficultyId",
            table: "RankedScores",
            column: "SongDifficultyId");

        migrationBuilder.CreateIndex(
            name: "IX_Scores_HmdId",
            table: "Scores",
            column: "HmdId");

        migrationBuilder.CreateIndex(
            name: "IX_Scores_LeftControllerId",
            table: "Scores",
            column: "LeftControllerId");

        migrationBuilder.CreateIndex(
            name: "IX_Scores_PlayerId",
            table: "Scores",
            column: "PlayerId");

        migrationBuilder.CreateIndex(
            name: "IX_Scores_RightControllerId",
            table: "Scores",
            column: "RightControllerId");

        migrationBuilder.CreateIndex(
            name: "IX_Scores_SetAt",
            table: "Scores",
            column: "SetAt");

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

        migrationBuilder.CreateIndex(
            name: "IX_TimeTicker_ExecutionTime",
            schema: "ticker",
            table: "TimeTickers",
            column: "ExecutionTime");

        migrationBuilder.CreateIndex(
            name: "IX_TimeTicker_Status_ExecutionTime",
            schema: "ticker",
            table: "TimeTickers",
            columns: new[] { "Status", "ExecutionTime" });

        migrationBuilder.CreateIndex(
            name: "IX_TimeTickers_ParentId",
            schema: "ticker",
            table: "TimeTickers",
            column: "ParentId");
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
            name: "CronTickerOccurrences",
            schema: "ticker");

        migrationBuilder.DropTable(
            name: "MapProposalAchievements");

        migrationBuilder.DropTable(
            name: "MapProposalCategories");

        migrationBuilder.DropTable(
            name: "MapProposalVersions");

        migrationBuilder.DropTable(
            name: "MapProposalVotes");

        migrationBuilder.DropTable(
            name: "MapRequestThreadBoosts");

        migrationBuilder.DropTable(
            name: "MapVersions");

        migrationBuilder.DropTable(
            name: "MemberAchievementStats");

        migrationBuilder.DropTable(
            name: "MemberPointStats");

        migrationBuilder.DropTable(
            name: "RankedMapRankedMapListAchievement");

        migrationBuilder.DropTable(
            name: "RankedScores");

        migrationBuilder.DropTable(
            name: "Sessions");

        migrationBuilder.DropTable(
            name: "TimeTickers",
            schema: "ticker");

        migrationBuilder.DropTable(
            name: "CronTickers",
            schema: "ticker");

        migrationBuilder.DropTable(
            name: "MapProposals");

        migrationBuilder.DropTable(
            name: "PlayModes");

        migrationBuilder.DropTable(
            name: "ContextMembers");

        migrationBuilder.DropTable(
            name: "Achievements");

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
            name: "MapRequestThreads");

        migrationBuilder.DropTable(
            name: "Controllers");

        migrationBuilder.DropTable(
            name: "SongDifficulties");

        migrationBuilder.DropTable(
            name: "Contexts");

        migrationBuilder.DropTable(
            name: "Players");

        migrationBuilder.DropTable(
            name: "GameModes");

        migrationBuilder.DropTable(
            name: "Songs");

        migrationBuilder.DropTable(
            name: "Guilds");

        migrationBuilder.DropTable(
            name: "Hmds");
    }
}
