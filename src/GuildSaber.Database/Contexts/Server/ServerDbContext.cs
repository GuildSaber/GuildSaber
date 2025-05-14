using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Boosts;
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

namespace GuildSaber.Database.Contexts.Server;

public class ServerDbContext : DbContext
{
    public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options) { }
    public ServerDbContext() { }

    public DbSet<Guild> Guilds { get; set; }
    public DbSet<GuildContext> GuildContexts { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Boost> Boosts { get; set; }
    public DbSet<Point> Points { get; set; }

    public DbSet<Player> Players { get; set; }

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

    protected override void OnModelCreating(ModelBuilder builder) => builder
        .ApplyConfiguration(new GuildConfiguration())
        .ApplyConfiguration(new GuildContextConfiguration())
        .ApplyConfiguration(new MemberConfiguration())
        .ApplyConfiguration(new BoostConfiguration())
        .ApplyConfiguration(new PointConfiguration())
        .ApplyConfiguration(new PlayerConfiguration())
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