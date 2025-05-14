using GuildSaber.Database.Models.DiscordBot;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Database.Contexts.DiscordBot;

public sealed class DiscordBotDbContext : DbContext
{
    public DiscordBotDbContext(DbContextOptions<DiscordBotDbContext> options) : base(options) { }
    public DiscordBotDbContext() { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<User>(x =>
            {
                x.HasKey(y => y.Id);
                x.Property(y => y.Id).ValueGeneratedNever();
            }
        );
}