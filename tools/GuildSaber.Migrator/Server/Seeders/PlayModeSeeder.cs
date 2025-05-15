using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class PlayModeSeeder
{
    public static readonly PlayMode[] PlayModes =
    [
        new()
        {
            Id = new PlayMode.PlayModeId(1),
            Name = "Standard"
        },
        new()
        {
            Id = new PlayMode.PlayModeId(2),
            Name = "ReBeat"
        },
        new()
        {
            Id = new PlayMode.PlayModeId(3),
            Name = "PinkPlay_Controllable"
        }
    ];

    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.PlayModes.AnyAsync(cancellationToken))
            return;

        dbContext.PlayModes.AddRange(PlayModes);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}