using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class PlayerSeeder
{
    public static readonly Player[] Players =
    [
        new()
        {
            Id = new Player.PlayerId(1),
            Info = new PlayerInfo
            {
                Username = "Kuurama",
                AvatarUrl = "https://avatars.akamai.steamstatic.com/641bb318819718248bb4570d4300949935052ccc_full.jpg",
                Country = "FR"
            },
            HardwareInfo = new PlayerHardwareInfo
            {
                HMD = PlayerHardwareInfo.EHMD.Quest3,
                Platform = PlayerHardwareInfo.EPlatform.Steam
            },
            LinkedAccounts = new PlayerLinkedAccounts
            {
                BeatLeaderId = BeatLeaderId.CreateUnsafe(76561198126131670).Value,
                DiscordId = null,
                ScoreSaberId = ScoreSaberId.CreateUnsafe(76561198126131670).Value
            },
            SubscriptionInfo = new PlayerSubscriptionInfo
            {
                Tier = PlayerSubscriptionInfo.ESubscriptionTier.None
            }
        }
    ];

    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Players.AnyAsync(cancellationToken))
            return;

        dbContext.Players.AddRange(Players);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}