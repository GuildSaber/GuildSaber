using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;

namespace GuildSaber.Api.Features.Players;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class PlayerMappers
{
    public static Expression<Func<Player, PlayerResponses.Player>> MapPlayerExpression
        => self => new PlayerResponses.Player
        {
            Id = self.Id,
            PlayerInfo = new PlayerResponses.PlayerInfo
            {
                Username = self.Info.Username,
                AvatarUrl = self.Info.AvatarUrl,
                Country = self.Info.Country,
                CreatedAt = self.Info.CreatedAt
            },
            PlayerHardwareInfo = new PlayerResponses.PlayerHardwareInfo
            {
                HMD = self.HardwareInfo.HMD.ToString(),
                Platform = self.HardwareInfo.Platform.ToString()
            },
            PlayerLinkedAccounts = new PlayerResponses.PlayerLinkedAccounts
            {
                BeatLeaderId = self.LinkedAccounts.BeatLeaderId.ToString(),
                DiscordId = self.LinkedAccounts.DiscordId.ToString(),
                ScoreSaberId = self.LinkedAccounts.ScoreSaberId.ToString()
            },
            PlayerSubscriptionInfo = new PlayerResponses.PlayerSubscriptionInfo
            {
                Tier = self.SubscriptionInfo.Tier.Map()
            }
        };

    public static Expression<Func<Player, PlayerResponses.PlayerAtMe>> MapPlayerAtMeExpression
        => self => new PlayerResponses.PlayerAtMe
        {
            Player = new PlayerResponses.Player
            {
                Id = self.Id,
                PlayerInfo = new PlayerResponses.PlayerInfo
                {
                    Username = self.Info.Username,
                    AvatarUrl = self.Info.AvatarUrl,
                    Country = self.Info.Country,
                    CreatedAt = self.Info.CreatedAt
                },
                PlayerHardwareInfo = new PlayerResponses.PlayerHardwareInfo
                {
                    HMD = self.HardwareInfo.HMD.ToString(),
                    Platform = self.HardwareInfo.Platform.ToString()
                },
                PlayerLinkedAccounts = new PlayerResponses.PlayerLinkedAccounts
                {
                    BeatLeaderId = self.LinkedAccounts.BeatLeaderId.ToString(),
                    DiscordId = self.LinkedAccounts.DiscordId.ToString(),
                    ScoreSaberId = self.LinkedAccounts.ScoreSaberId.ToString()
                },
                PlayerSubscriptionInfo = new PlayerResponses.PlayerSubscriptionInfo
                {
                    Tier = self.SubscriptionInfo.Tier.Map()
                }
            },
            Members = self.Members.Select(x => new MemberResponses.Member
            {
                GuildId = x.GuildId,
                PlayerId = x.PlayerId,
                JoinState = x.JoinState.Map(),
                Permissions = x.Permissions.Map(),
                EditedAt = x.EditedAt,
                InitializedAt = x.CreatedAt,
                Priority = x.Priority
            }).ToArray(),
            Roles = self.IsManager ? new[] { "Manager" } : Array.Empty<string>()
        };

    public static PlayerResponses.ESubscriptionTier Map(this PlayerSubscriptionInfo.ESubscriptionTier self) =>
        self switch
        {
            PlayerSubscriptionInfo.ESubscriptionTier.None => PlayerResponses.ESubscriptionTier.None,
            PlayerSubscriptionInfo.ESubscriptionTier.Tier1 => PlayerResponses.ESubscriptionTier.Tier1,
            PlayerSubscriptionInfo.ESubscriptionTier.Tier2 => PlayerResponses.ESubscriptionTier.Tier2,
            PlayerSubscriptionInfo.ESubscriptionTier.Tier3 => PlayerResponses.ESubscriptionTier.Tier3,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };
}