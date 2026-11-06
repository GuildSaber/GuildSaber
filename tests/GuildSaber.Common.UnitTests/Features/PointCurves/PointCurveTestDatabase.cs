using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Common.UnitTests.Utils;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using GuildSaber.Database.Models.StrongTypes;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Color = System.Drawing.Color;
using Context = GuildSaber.Database.Models.Server.Guilds.Context;
using EModifiers = GuildSaber.Database.Models.Server.Scores.AbstractScore.EModifiers;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

internal sealed class PointCurveTestDatabase(DbContextOptions<ServerDbContext> options) : IAsyncDisposable
{
    public ServerDbContext Context { get; } = new(options);

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Context.Database.EnsureDeletedAsync();
        }
        finally
        {
            await Context.DisposeAsync();
        }
    }

    public ServerDbContext CreateContext(DbCommandInterceptor? interceptor = null, bool retry = false)
    {
        var builder = new DbContextOptionsBuilder<ServerDbContext>(options);

        if (retry)
            builder.UseNpgsql(npgsql => npgsql.EnableRetryOnFailure(2, TimeSpan.Zero, null));

        if (interceptor is not null)
            builder.AddInterceptors(interceptor);

        return new ServerDbContext(builder.Options);
    }

    public static async Task<PointCurveTestDatabase> CreateAsync(bool useMigrations = false)
    {
        var server = await PostgresTestServer.GetConnectionStringAsync();

        var connection = new NpgsqlConnectionStringBuilder(server)
        {
            Database = $"guildsaber_point_curve_test_{Guid.NewGuid():N}",
            Pooling = false
        };

        var builder = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql(connection.ConnectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        builder.WithExpressionExpanding();

        var database = new PointCurveTestDatabase(builder.Options);

        try
        {
            if (useMigrations)
                await database.Context.Database.MigrateAsync();
            else
                await database.Context.Database.EnsureCreatedAsync();

            await SeedAsync(database.Context);

            return database;
        }
        catch
        {
            await database.DisposeAsync();

            throw;
        }
    }

    private static async Task SeedAsync(ServerDbContext db)
    {
        var now = new DateTimeOffset(2026, 10, 29, 0, 0, 0, TimeSpan.Zero);

        db.AddRange(
            new Guild
            {
                Id = new GuildId(2),
                Info = GuildInfo.TryCreate(
                    "BeatSaber Challenge Community", "BSCC", "Isolated CAP fixture", Color.White, now).Value,
                Requirements = default,
                Status = Guild.EGuildStatus.Unverified,
                DiscordInfo = default
            },
            new Context
            {
                Id = new ContextId(2),
                GuildId = new GuildId(2),
                Info = new ContextInfo("Main", "")
            },
            new Player
            {
                Id = new PlayerId(1),
                Info = new PlayerInfo("Player", "", "", now),
                Platform = default,
                LinkedAccounts = default,
                SubscriptionInfo = default
            },
            new Member
            {
                GuildId = new GuildId(2),
                PlayerId = new PlayerId(1),
                CreatedAt = now,
                EditedAt = now,
                Permissions = default,
                JoinState = Member.EJoinState.Joined,
                Priority = 0
            },
            new ContextMember
            {
                GuildId = new GuildId(2),
                ContextId = new ContextId(2),
                PlayerId = new PlayerId(1)
            },
            new GameMode
            {
                Id = new GameMode.GameModeId(1),
                Name = "Standard"
            },
            new PlayMode
            {
                Id = new PlayMode.PlayModeId(1),
                Name = "Standard"
            },
            new Song
            {
                Id = new Song.SongId(1),
                Hash = SongHash.CreateUnsafe(new string('a', 40)).Value,
                UploadedAt = now,
                Info = new SongInfo("Fixture", "Fixture", "", "Author", "Mapper")
            },
            new SongDifficulty
            {
                Id = new SongDifficulty.SongDifficultyId(1),
                SongId = new Song.SongId(1),
                GameModeId = new GameMode.GameModeId(1),
                Difficulty = EDifficulty.ExpertPlus,
                Stats = new SongDifficultyStats(
                    MaxScore.TryCreate(1000).Value, NJS.CreateUnsafe(10).Value, 10, 0, 0, 1, 10)
            },
            new Point
            {
                Id = new Point.PointId(3),
                GuildId = new GuildId(2),
                Info = PointInfo.TryCreate("CAP", "Experimental local-context percentage accuracy bonus").Value,
                CurveSettings = new CurveSettings
                {
                    Difficulty = new CustomCurve([(32, 100), (0, 100)]),
                    Accuracy = new CustomCurve([(1, 1), (0, 0)]),
                    AccuracyBonusRatio = 0.25,
                    MapCurvePolicy = PointCurvePolicy.EditableMapCurve
                }
            },
            new MapRequestThread
            {
                Id = new MapRequestThreadId(1),
                ContextId = new ContextId(2),
                CreatedAt = now
            },
            new MapVersion
            {
                RankedMapId = new RankedMapId(1),
                SongDifficultyId = new SongDifficulty.SongDifficultyId(1),
                SongId = new Song.SongId(1),
                PlayModeId = new PlayMode.PlayModeId(1),
                AddedAt = now
            },
            new RankedMap
            {
                Id = new RankedMapId(1),
                GuildId = new GuildId(2),
                ContextId = new ContextId(2),
                ThreadId = new MapRequestThreadId(1),
                Info = new RankedMapInfo(now, now),
                Requirements = new RankedMapRequirements(
                    false, false, null,
                    EModifiers.ProhibitedDefaults, EModifiers.None, null),
                Rating = new RankedMapRating
                {
                    AccStar = new RankedMapRating.AccuracyStar(5),
                    DiffStar = new RankedMapRating.DifficultyStar(20)
                }
            });

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var context = await db.Contexts
            .AsTracking()
            .Include(x => x.Points)
            .SingleAsync();

        context.Points.Add(await db.Points
            .AsTracking()
            .SingleAsync());

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    public async Task AddPlayerAsync(int id)
    {
        var now = DateTimeOffset.UtcNow;

        Context.AddRange(new Player
        {
            Id = new PlayerId(id),
            Info = new PlayerInfo($"Player {id}", "", "", now),
            Platform = default,
            LinkedAccounts = default,
            SubscriptionInfo = default
        }, new Member
        {
            GuildId = new GuildId(2),
            PlayerId = new PlayerId(id),
            CreatedAt = now,
            EditedAt = now,
            Permissions = default,
            JoinState = Member.EJoinState.Joined,
            Priority = 0
        }, new ContextMember
        {
            GuildId = new GuildId(2),
            ContextId = new ContextId(2),
            PlayerId = new PlayerId(id)
        });

        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public async Task<AbstractScore> AddScoreAsync(
        int id,
        int baseScore,
        EModifiers modifiers = EModifiers.None,
        bool beatLeader = true,
        int playerId = 1,
        bool? isWin = null)
    {
        var statistics = isWin is null
            ? null
            : new ScoreStatistics
            {
                WinTracker = new WinTracker(isWin.Value, 1, null, 1, baseScore, 1000)
                {
                    PauseStatistics = null,
                    AverageHeadPosition = null
                },
                HitTracker = new HitTracker(null, 0, 0, 0, 0, 0, 0, 0, 0),
                AccuracyTracker = new AccuracyTracker(0, 0, 0, 0, 0, 0, 0, 0, [], [], []),
                ScoreGraphTracker = new ScoreGraphTracker([])
            };

        AbstractScore score = beatLeader
            ? new BeatLeaderScore
            {
                Id = new ScoreId(id),
                BeatLeaderScoreId = BeatLeaderScoreId.CreateUnsafe(id),
                PlayerId = new PlayerId(playerId),
                SongDifficultyId = new SongDifficulty.SongDifficultyId(1),
                BaseScore = BaseScore.TryCreate(baseScore).Value,
                Modifiers = modifiers,
                SetAt = DateTimeOffset.UtcNow.AddSeconds(id),
                MaxCombo = null,
                IsFullCombo = false,
                MissedNotes = 0,
                BadCuts = 0,
                Statistics = statistics
            }
            : new ScoreSaberScore
            {
                Id = new ScoreId(id),
                ScoreSaberScoreId = ScoreSaberScoreId.CreateUnsafe(id).Value,
                PlayerId = new PlayerId(playerId),
                SongDifficultyId = new SongDifficulty.SongDifficultyId(1),
                BaseScore = BaseScore.TryCreate(baseScore).Value,
                Modifiers = modifiers,
                SetAt = DateTimeOffset.UtcNow.AddSeconds(id),
                MaxCombo = null,
                IsFullCombo = false,
                MissedNotes = 0,
                BadCuts = 0,
                Statistics = statistics
            };

        Context.Scores.Add(score);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        return score;
    }
}