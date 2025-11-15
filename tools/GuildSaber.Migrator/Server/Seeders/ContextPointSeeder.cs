using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class ContextPointSeeder
{
    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Contexts.AnyAsync(x => x.Points.Any(), cancellationToken: cancellationToken))
            return;

        var guildContexts = await dbContext.Contexts
            .AsTracking()
            .Where(x => !x.Points.Any())
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (var guildContext in guildContexts)
        {
            var points = await dbContext.Points.Where(x => x.GuildId == guildContext.GuildId)
                .ToArrayAsync(cancellationToken);

            guildContext.Points = points;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}