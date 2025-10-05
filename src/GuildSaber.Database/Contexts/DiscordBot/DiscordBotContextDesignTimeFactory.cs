using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GuildSaber.Database.Contexts.DiscordBot;

[UsedImplicitly(Reason = "Used by EF Core to create a design-time DbContext")]
public class DiscordBotContextDesignTimeFactory : IDesignTimeDbContextFactory<DiscordBotDbContext>
{
    public DiscordBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DiscordBotDbContext>();
        optionsBuilder.UseNpgsql("Server=none;Database=none;");
        return new DiscordBotDbContext(optionsBuilder.Options);
    }
}