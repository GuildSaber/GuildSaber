using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace GuildSaber.Database.Contexts.DiscordBot;

[UsedImplicitly(Reason = "Used by EF Core to create a design-time DbContext")]
public class DiscordBotContextDesignTimeFactory : IDesignTimeDbContextFactory<DiscordBotDbContext>
{
    public DiscordBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DiscordBotDbContext>();
        optionsBuilder.UseMySql("Server=none;Database=none;", ServerVersion.Create(10, 6, 18, ServerType.MariaDb));
        return new DiscordBotDbContext(optionsBuilder.Options);
    }
}