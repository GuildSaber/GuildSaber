using GuildSaber.Database.Models.Guild;
using GuildSaber.Database.Models.Guild.Navigation;
using GuildSaber.Database.Models.Player;
using GuildSaber.Database.Models.Song;
using GuildSaber.Database.Models.SongDifficulty;
using GuildSaber.Database.Models.SongDifficulty.Navigation;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Database.Contexts;

public class GSDbContext : DbContext
{
    public GSDbContext(DbContextOptions<GSDbContext> options) : base(options) { }
    public GSDbContext() { }
    
    public DbSet<Guild> Guilds { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<SongDifficulty> SongDifficulties { get; set; }
    public DbSet<GameMode> GameModes { get; set; }

    public DbSet<Member> Members { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new GuildConfiguration())
            .ApplyConfiguration(new PlayerConfiguration())
            .ApplyConfiguration(new MemberConfiguration())
            .ApplyConfiguration(new SongConfiguration())
            .ApplyConfiguration(new SongDifficultyConfiguration())
            .ApplyConfiguration(new GameModeConfiguration());
    }
}