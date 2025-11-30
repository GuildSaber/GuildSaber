using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Auth;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Boosts;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.RankedScores;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GuildSaber.Database.Contexts.Server;

public class ServerDbContext : DbContext
{
    public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options) { }
    public ServerDbContext() { }

    public DbSet<Guild> Guilds { get; set; }
    public DbSet<Context> Contexts { get; set; }
    public DbSet<Level> Levels { get; set; }
    public DbSet<ContextMember> ContextMembers { get; set; }

    public DbSet<Member> Members { get; set; }
    public DbSet<MemberPointStat> MemberPointStats { get; set; }
    public DbSet<MemberLevelStat> MemberLevelStats { get; set; }

    public DbSet<Boost> Boosts { get; set; }
    public DbSet<Point> Points { get; set; }
    public DbSet<Category> Categories { get; set; }

    public DbSet<Player> Players { get; set; }
    public DbSet<Session> Sessions { get; set; }

    public DbSet<RankedMap> RankedMaps { get; set; }
    public DbSet<MapVersion> MapVersions { get; set; }
    public DbSet<PlayMode> PlayModes { get; set; }

    public DbSet<AbstractScore> Scores { get; set; }
    public DbSet<ScoreSaberScore> ScoreSaberScores { get; set; }
    public DbSet<BeatLeaderScore> BeatLeaderScores { get; set; }

    public DbSet<RankedScore> RankedScores { get; set; }

    public DbSet<Song> Songs { get; set; }
    public DbSet<SongDifficulty> SongDifficulties { get; set; }
    public DbSet<GameMode> GameModes { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Remove<ComplexTypeAttributeConvention>();
        configurationBuilder.Conventions.Add(services => new EFCoreComplexTypeConventionColumnNameShortener(
            services.GetRequiredService<ProviderConventionSetBuilderDependencies>())
        );
    }

    /// <remarks>
    /// Sadly, can't use ApplyConfigurationsFromAssembly because there isn't only one DbContext registering
    /// configurations in this assembly.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder builder) => builder
        .ApplyConfiguration(new GuildConfiguration())
        .ApplyConfiguration(new ContextConfiguration())
        .ApplyConfiguration(new LevelConfiguration())
        .ApplyConfiguration(new RankedMapListLevelConfiguration())
        .ApplyConfiguration(new DiffStarLevelConfiguration())
        .ApplyConfiguration(new AccStarLevelConfiguration())
        .ApplyConfiguration(new MemberConfiguration())
        .ApplyConfiguration(new MemberPointStatConfiguration())
        .ApplyConfiguration(new MemberLevelStatConfiguration())
        .ApplyConfiguration(new ContextMemberConfiguration())
        .ApplyConfiguration(new BoostConfiguration())
        .ApplyConfiguration(new PointConfiguration())
        .ApplyConfiguration(new CategoryConfiguration())
        .ApplyConfiguration(new PlayerConfiguration())
        .ApplyConfiguration(new SessionConfiguration())
        .ApplyConfiguration(new AbstractScoreConfiguration())
        .ApplyConfiguration(new ScoreSaberScoreConfiguration())
        .ApplyConfiguration(new BeatLeaderScoreConfiguration())
        .ApplyConfiguration(new RankedMapConfiguration())
        .ApplyConfiguration(new MapVersionConfiguration())
        .ApplyConfiguration(new RankedScoreConfiguration())
        .ApplyConfiguration(new SongConfiguration())
        .ApplyConfiguration(new SongDifficultyConfiguration())
        .ApplyConfiguration(new PlayModeConfiguration())
        .ApplyConfiguration(new GameModeConfiguration());
}