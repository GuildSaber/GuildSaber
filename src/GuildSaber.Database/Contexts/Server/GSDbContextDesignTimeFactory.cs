using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GuildSaber.Database.Contexts.Server;

[UsedImplicitly(Reason = "Used by EF Core to create a design-time DbContext")]
public class GSDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ServerDbContext>
{
    public ServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>();
        optionsBuilder.UseNpgsql("Server=none;Database=none;");
        return new ServerDbContext(optionsBuilder.Options);
    }
}