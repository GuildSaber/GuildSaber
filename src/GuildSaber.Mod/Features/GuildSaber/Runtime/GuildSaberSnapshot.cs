using System.Collections.Immutable;
using static GuildSaber.Api.Features.Guilds.Http.GuildResponses;
using static GuildSaber.Api.Features.Guilds.Members.ContextStats.Http.ContextStatResponses;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.Http.LevelStatResponses;
using static GuildSaber.Api.Features.Players.Http.PlayerResponses;

namespace GuildSaber.Mod.Features.GuildSaber.Runtime;

public sealed record GuildSaberSnapshot(
    PlayerExtended PlayerExtended,
    ImmutableArray<GuildExtended> AvailableGuilds,
    GuildExtended CurrentGuildExtended,
    ContextId CurrentContextId,
    ImmutableArray<MemberLevelStat> LevelStats,
    MemberContextStat ContextStats
)
{
    public PlayerId PlayerId => PlayerExtended.Player.Id;
}