using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class GameModeSeeder
{
    public static readonly GameMode[] GameModes =
    [
        new()
        {
            Id = new GameMode.GameModeId(1),
            Name = "Standard"
        },
        new()
        {
            Id = new GameMode.GameModeId(2),
            Name = "Lawless"
        },
        new()
        {
            Id = new GameMode.GameModeId(3),
            Name = "OneSaber"
        },
        new()
        {
            Id = new GameMode.GameModeId(4),
            Name = "90Degree"
        },
        new()
        {
            Id = new GameMode.GameModeId(5),
            Name = "360Degree"
        },
        new()
        {
            Id = new GameMode.GameModeId(6),
            Name = "NoArrows"
        },
        new()
        {
            Id = new GameMode.GameModeId(7),
            Name = "Generated360Degree"
        },
        new()
        {
            Id = new GameMode.GameModeId(8),
            Name = "Generated90Degree"
        }
    ];

    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.GameModes.AnyAsync(cancellationToken))
            return;

        dbContext.GameModes.AddRange(GameModes);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}