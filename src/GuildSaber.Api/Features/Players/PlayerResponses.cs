using System.Text.Json.Serialization;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;

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