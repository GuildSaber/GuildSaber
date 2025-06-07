using GuildSaber.Api.Features.Guilds.Members;

namespace GuildSaber.Api.Features.Players;

public static class PlayerResponses
{
    public readonly record struct Player(
        uint Id,
        PlayerHardwareInfo PlayerHardwareInfo,
        PlayerInfo PlayerInfo,
        PlayerLinkedAccounts PlayerLinkedAccounts,
        PlayerSubscriptionInfo PlayerSubscriptionInfo
    );

    public readonly record struct PlayerHardwareInfo(
        string HMD,
        string Platform
    );

    public readonly record struct PlayerInfo(
        string Username,
        string AvatarUrl,
        string Country,
        DateTimeOffset CreatedAt
    );

    public readonly record struct PlayerLinkedAccounts(
        string BeatLeaderId,
        string? ScoreSaberId,
        string? DiscordId
    );

    public readonly record struct PlayerSubscriptionInfo(
        ESubscriptionTier Tier
    );

    public enum ESubscriptionTier
    {
        None = 0,
        Tier1 = 1 << 0,
        Tier2 = 1 << 1,
        Tier3 = 1 << 2
    }

    public readonly record struct PlayerAtMe(
        Player Player,
        MemberResponses.Member[] Members,
        string[] Roles
    );
}