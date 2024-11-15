namespace GuildSaber.Database.Models.Player;

public readonly record struct PlayerSubscriptionInfo(
    PlayerSubscriptionInfo.ESubscriptionTier Tier
)
{
    public enum ESubscriptionTier
    {
        None = 0,
        Tier1 = 1 << 0,
        Tier2 = 1 << 1,
        Tier3 = 1 << 2
    }
}