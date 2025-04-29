using GuildSaber.Database.Models;
using GuildSaber.Database.Models.Guilds;
using GuildSaber.Database.Models.Guilds.Boosts;
using GuildSaber.Database.Models.Guilds.Members;
using GuildSaber.Database.Models.Players;
using GuildSaber.Database.Models.Songs;
using GuildSaber.Database.Models.Songs.SongDifficulties;
using GuildSaber.Database.Models.Songs.SongDifficulties.GameModes;
using GuildSaber.Database.Models.Songs.SongDifficulties.PlayModes;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Database.Contexts;

public class GSDbContext : DbContext
{
    public GSDbContext(DbContextOptions<GSDbContext> options) : base(options) { }
    public GSDbContext() { }
    
    public DbSet<Guild> Guilds { get; set; }
    public DbSet<GuildContext> GuildContexts { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Boost> Boosts { get; set; }
    
    public DbSet<Player> Players { get; set; }
    
    public DbSet<Song> Songs { get; set; }
    public DbSet<SongDifficulty> SongDifficulties { get; set; }
    public DbSet<GameMode> GameModes { get; set; }
    public DbSet<PlayMode> PlayModes { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new GuildConfiguration())
            .ApplyConfiguration(new PlayerConfiguration())
            .ApplyConfiguration(new MemberConfiguration())
            .ApplyConfiguration(new SongConfiguration())
            .ApplyConfiguration(new SongDifficultyConfiguration())
            .ApplyConfiguration(new GameModeConfiguration())
            .ApplyConfiguration(new PlayModeConfiguration())
            .ApplyConfiguration(new GuildContextConfiguration());
    }
}