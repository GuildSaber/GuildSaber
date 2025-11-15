using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class ContextMemberSeeder
{
    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.ContextMembers.AnyAsync(cancellationToken: cancellationToken))
            return;

        var guildContexts = await dbContext.GuildContexts
            .AsTracking()
            .Where(x => !x.Members.Any())
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (var guildContext in guildContexts)
        {
            var members = await dbContext.Members
                .Where(x => x.GuildId == guildContext.GuildId)
                .ToArrayAsync(cancellationToken);

            guildContext.Members = members;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}