using System.Text.Json.Serialization;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Api.Features.Hardware.Http;

namespace GuildSaber.Api.Features.Players.Http;

public static class PlayerResponses
{
    public record Player(
        PlayerId Id,
        PlayerInfo PlayerInfo,
        PlayerLinkedAccounts PlayerLinkedAccounts,
        PlayerSubscriptionInfo PlayerSubscriptionInfo,
        HardwareResponses.Hmd? Hmd,
        string Platform,
        bool IsManager
    );

    public readonly record struct PlayerInfo(
        string Username,
        string AvatarUrl,
        string Country,
        DateTimeOffset CreatedAt
    );

    public readonly record struct PlayerLinkedAccounts(
        SteamId? SteamId,
        MetaPCId? MetaPCId,
        BLNativeId? BLNativeId,
        ScoreSaberId? ScoreSaberId,
        DiscordId? DiscordId
    )
    {
        [JsonIgnore]
        public BeatLeaderId BeatLeaderId => ((BeatLeaderId?)SteamId ?? (BeatLeaderId?)MetaPCId ?? BLNativeId)!.Value;
    }

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