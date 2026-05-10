using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;

namespace GuildSaber.Api.Features.Players;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class PlayerMappers
{
    private static Func<Player, PlayerResponses.Player>? _mapPlayerImpl;

    public static Expression<Func<Player, PlayerResponses.Player>> MapPlayerExpression
        => self => new PlayerResponses.Player(
            self.Id,
            new PlayerResponses.PlayerInfo(
                self.Info.Username,
                self.Info.AvatarUrl,
                self.Info.Country,
                self.Info.CreatedAt
            ),
            new PlayerResponses.PlayerHardwareInfo(
                self.HardwareInfo.HMD.ToString(),
                self.HardwareInfo.Platform.ToString()
            ),
            new PlayerResponses.PlayerLinkedAccounts(
                self.LinkedAccounts.BeatLeaderId.ToString(),
                self.LinkedAccounts.ScoreSaberId.ToString(),
                self.LinkedAccounts.DiscordId.ToString()
            ),
            new PlayerResponses.PlayerSubscriptionInfo(
                self.SubscriptionInfo.Tier.Map()
            ),
            self.IsManager
        );

    public static Expression<Func<Player, PlayerResponses.PlayerExtended>> MapPlayerExtendedExpression
        => self => new PlayerResponses.PlayerExtended(new PlayerResponses.Player(
                self.Id,
                new PlayerResponses.PlayerInfo(
                    self.Info.Username,
                    self.Info.AvatarUrl,
                    self.Info.Country,
                    self.Info.CreatedAt
                ),
                new PlayerResponses.PlayerHardwareInfo(
                    self.HardwareInfo.HMD.ToString(),
                    self.HardwareInfo.Platform.ToString()
                ),
                new PlayerResponses.PlayerLinkedAccounts(
                    self.LinkedAccounts.BeatLeaderId.ToString(),
                    self.LinkedAccounts.ScoreSaberId.ToString(),
                    self.LinkedAccounts.DiscordId.ToString()
                ),
                new PlayerResponses.PlayerSubscriptionInfo(
                    self.SubscriptionInfo.Tier.Map()
                ),
                self.IsManager),
            self.Members.Select(x => new MemberResponses.Member(
                x.PlayerId,
                x.GuildId,
                x.CreatedAt,
                x.EditedAt,
                x.Permissions.Map(),
                x.JoinState.Map(),
                x.Priority
            )).ToArray());

    public static PlayerResponses.Player Map(this Player self)
    {
        _mapPlayerImpl ??= MapPlayerExpression.Compile();
        return _mapPlayerImpl(self);
    }

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