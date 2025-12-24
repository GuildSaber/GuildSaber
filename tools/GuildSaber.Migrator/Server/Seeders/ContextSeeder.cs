using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class ContextSeeder
{
    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Contexts.AnyAsync(cancellationToken))
            return;

        dbContext.Contexts.AddRange(
            new Context
            {
                Id = new ContextId(1),
                GuildId = new GuildId(1),
                Type = Context.EContextType.Default,
                Info = new ContextInfo
                {
                    Description = "The main context of the guild, where all the maps are ranked.",
                    Name = "General"
                }
            },
            new Context
            {
                Id = new ContextId(2),
                GuildId = new GuildId(2),
                Type = Context.EContextType.Default,
                Info = new ContextInfo
                {
                    Description = "The main context of the guild, where all the maps are ranked.",
                    Name = "General"
                }
            });
    }
}