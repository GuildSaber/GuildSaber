using System.Drawing;
using GuildSaber.Common.Result;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class GuildSeeder
{
    public static readonly Guild[] DefaultGuilds =
    [
        new()
        {
            Id = new Guild.GuildId(1),
            Info = GuildInfo.TryCreate(
                name: "Challenge Saber",
                smallName: "CS",
                description: "A Level based ranking with category support."
                             + "Come and fight for who’s the best in Vibro, Tech, Shitpost or Jumps!",
                color: Color.FromArgb(255, 166, 19, 111),
                createdAt: DateTimeOffset.UtcNow
            ).Unwrap(),
            Requirements = new GuildRequirements
            {
                RequireSubmission = false
            },
            Status = Guild.EGuildStatus.Featured
        },
        new()
        {
            Id = new Guild.GuildId(2),
            Info = GuildInfo.TryCreate(
                name: "Beat Saber Challenge Community",
                smallName: "BSCC",
                description: "The legacy of the challenge ranking, a higher mapping standard, a loving community."
                             + "Why waiting? Join-us!",
                color: Color.FromArgb(255, 255, 124, 0),
                createdAt: DateTimeOffset.UtcNow
            ).Unwrap(),
            Status = Guild.EGuildStatus.Featured,
            Requirements = new GuildRequirements
            {
                RequireSubmission = false,
                MinRank = 200,
                MaxRank = 10,
                MinPP = 0,
                MaxPP = 10000,
                AccountAgeUnix = TimeSpan.FromDays(365 * 6).Seconds
            }
        }
    ];

    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Guilds.AnyAsync(cancellationToken))
            return;

        await dbContext.Guilds.AddRangeAsync(DefaultGuilds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}