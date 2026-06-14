using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GuildSaber.Common.StrongTypes;
using static GuildSaber.Api.Features.Guilds.Http.GuildResponses;
using static GuildSaber.Api.Features.Guilds.Members.ContextStats.Http.ContextStatResponses;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.Http.LevelStatResponses;
using static GuildSaber.Api.Features.Players.Http.PlayerResponses;

namespace GuildSaber.Mod.Features.GuildSaber.Runtime;

public sealed class GuildSaberSession
{
    private readonly Dictionary<GuildId, GuildExtended> _guildsExtended = [];
    private readonly Dictionary<ContextId, MemberContextStat> _memberContextStats = [];
    private readonly Dictionary<ContextId, MemberLevelStat[]> _memberLevelStats = [];
    private ContextId? _currentContextId;
    private PlayerExtended? _player;

    public PlayerExtended PlayerExtended
        => _player ?? throw new InvalidOperationException("GuildSaber local player is not loaded.");

    public PlayerId PlayerId => PlayerExtended.Player.Id;
    public bool HasLocalPlayer => _player is not null;
    public bool HasAvailableGuilds => _guildsExtended.Count > 0;

    public bool HasCurrentMemberStats
        => TryGetCurrentContextId(out var contextId)
           && _memberLevelStats.ContainsKey(contextId)
           && _memberContextStats.ContainsKey(contextId);

    public GuildExtended CurrentGuild
    {
        get => field ?? throw new InvalidOperationException("GuildSaber current guild is not selected.");
        private set;
    }

    public ContextId CurrentContextId
    {
        get => _currentContextId switch
        {
            { Value: > 0 } contextId => contextId,
            _ => throw new InvalidOperationException("GuildSaber current context is not selected.")
        };
        private set => _currentContextId = value;
    }

    public MemberLevelStat[] CurrentMemberLevelStats
        => TryGetCurrentMemberLevelStats(out var levelStats)
            ? levelStats
            : throw new InvalidOperationException("GuildSaber current member level stats are not loaded.");

    public MemberContextStat CurrentMemberContextStats
        => TryGetCurrentMemberContextStats(out var contextStats)
            ? contextStats
            : throw new InvalidOperationException("GuildSaber current member context stats are not loaded.");

    public event Action<GuildExtended, ContextId> CurrentGuildContextChanged = (_, _) => { };

    public void SetPlayer(PlayerExtended player) => _player = player;

    public void SetGuilds(IEnumerable<GuildExtended> guilds)
    {
        _guildsExtended.Clear();
        foreach (var guild in guilds) _guildsExtended[guild.Guild.Id] = guild;
    }

    public void SetCurrentGuildContext(GuildExtended guild, ContextId contextId)
    {
        CurrentGuild = guild;
        CurrentContextId = contextId;

        CurrentGuildContextChanged.Invoke(guild, contextId);
    }

    public void SetMemberStats(
        ContextId contextId,
        MemberLevelStat[] levelStats,
        MemberContextStat contextStats)
    {
        _memberLevelStats[contextId] = levelStats;
        _memberContextStats[contextId] = contextStats;
    }

    public bool TryGetGuild(GuildId guildId, out GuildExtended guild)
        => _guildsExtended.TryGetValue(guildId, out guild!);

    public GuildExtended GetGuild(GuildId guildId) => TryGetGuild(guildId, out var guild) switch
    {
        true => guild,
        _ => throw new InvalidOperationException($"GuildSaber guild {guildId.Value} is not loaded.")
    };

    public GuildExtended[] GetAvailableGuilds() => _guildsExtended.Values.ToArray();

    public bool TryGetAvailableGuildAt(int index, [NotNullWhen(true)] out GuildExtended? guild)
    {
        if (index < 0 || index >= _guildsExtended.Count)
        {
            guild = null;
            return false;
        }

        guild = _guildsExtended.Values.ElementAt(index);
        return true;
    }

    public bool TryGetCurrentMemberLevelStats(out MemberLevelStat[] levelStats)
    {
        if (TryGetCurrentContextId(out var contextId)
            && _memberLevelStats.TryGetValue(contextId, out var currentLevelStats))
        {
            levelStats = currentLevelStats;
            return true;
        }

        levelStats = [];
        return false;
    }

    public bool TryGetCurrentMemberContextStats(out MemberContextStat contextStats)
    {
        if (TryGetCurrentContextId(out var contextId)
            && _memberContextStats.TryGetValue(contextId, out var currentContextStats))
        {
            contextStats = currentContextStats;
            return true;
        }

        contextStats = default;
        return false;
    }

    private bool TryGetCurrentContextId(out ContextId contextId)
    {
        if (_currentContextId is { Value: > 0 } currentContextId)
        {
            contextId = currentContextId;
            return true;
        }

        contextId = default;
        return false;
    }
}