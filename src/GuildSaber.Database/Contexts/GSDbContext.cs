using GuildSaber.Database.Models.Guild;
using GuildSaber.Database.Models.Guild.Navigation;
using GuildSaber.Database.Models.Player;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Database.Contexts;

public class GSDbContext : DbContext
{
    public DbSet<Guild> Guilds { get; set; }
    public DbSet<Player> Players { get; set; }
    
    public DbSet<Member> Members { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new GuildConfiguration())
            .ApplyConfiguration(new PlayerConfiguration())
            .ApplyConfiguration(new MemberConfiguration());
    }
}