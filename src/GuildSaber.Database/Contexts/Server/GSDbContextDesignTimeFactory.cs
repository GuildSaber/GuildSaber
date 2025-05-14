using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace GuildSaber.Database.Contexts.Server;

[UsedImplicitly(Reason = "Used by EF Core to create a design-time DbContext")]
public class GSDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ServerDbContext>
{
    public ServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>();
        optionsBuilder.UseMySql("Server=none;Database=none;", ServerVersion.Create(10, 6, 18, ServerType.MariaDb));
        return new ServerDbContext(optionsBuilder.Options);
    }
}