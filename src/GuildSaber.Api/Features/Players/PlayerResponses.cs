using GuildSaber.Api.Features.Guilds.Members;

namespace GuildSaber.Api.Features.Players;

public static class PlayerResponses
{
    public record Player(
        PlayerId Id,
        PlayerInfo PlayerInfo,
        PlayerHardwareInfo PlayerHardwareInfo,
        PlayerLinkedAccounts PlayerLinkedAccounts,
        PlayerSubscriptionInfo PlayerSubscriptionInfo,
        bool IsManager
    );

    public readonly record struct PlayerInfo(
        string Username,
        string AvatarUrl,
        string Country,
        DateTimeOffset CreatedAt
    );

    public readonly record struct PlayerHardwareInfo(
        string HMD,
        string Platform
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
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3
    }

    public record PlayerExtended(
        Player Player,
        MemberResponses.Member[] Members
    );
}