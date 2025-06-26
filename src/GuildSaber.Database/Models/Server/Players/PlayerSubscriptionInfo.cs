namespace GuildSaber.Database.Models.Server.Players;

public readonly record struct PlayerSubscriptionInfo(
    PlayerSubscriptionInfo.ESubscriptionTier Tier
)
{
    public enum ESubscriptionTier
    {
        None = 0,
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3
    }
}