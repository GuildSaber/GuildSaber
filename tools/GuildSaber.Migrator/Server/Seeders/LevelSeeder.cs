using System.Drawing;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class LevelSeeder
{
    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Levels.AnyAsync(cancellationToken))
            return;

        foreach (var diff in Enumerable.Range(1, 34))
            dbContext.Levels.Add(new GlobalLevel
            {
                ContextId = new Context.ContextId(1),
                GuildId = new Guild.GuildId(1),
                Info = new LevelInfo($"Level {diff}", Color.Black),
                NeedCompletion = true,
                Order = (uint)diff,
                Requirement = new LevelRequirement
                {
                    Type = LevelRequirement.ELevelRequirementType.DiffStar,
                    MinDiffStar = new RankedMapRating.DifficultyStar(diff),
                    MinPassCount = 1
                }
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}