using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using OculusStudios.Platform.Core;
using Zenject;
using static GuildSaber.Api.Features.Guilds.Members.ContextStats.Http.ContextStatResponses;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.Http.LevelStatResponses;
using static GuildSaber.Api.Features.Players.Http.PlayerResponses;
using static GuildSaber.Mod.Features.GuildSaber.Runtime.GuildSaberRuntimeState;

namespace GuildSaber.Mod.Features.GuildSaber.Runtime;

public sealed class GuildSaberManager(
    GuildSaberClient client,
    GuildSaberConfig config,
    IPlatform platform,
    Logger logger
) : IInitializable, IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _activeTransition;
    private ImmutableArray<GuildResponses.GuildExtended> _availableGuilds = [];
    private PlayerExtended? _playerExtended;

    public GuildSaberRuntimeState State { get; private set; } = new Loading();
    public event Action<GuildSaberRuntimeState>? StateChanged;

    private readonly record struct MemberStats(
        ImmutableArray<MemberLevelStat> LevelStats,
        MemberContextStat ContextStats
    );

    void IInitializable.Initialize() => _ = ReInitializeAsync();

    public void Dispose()
    {
        _activeTransition?.Cancel();
        _activeTransition = null;

        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    public Task ReInitializeAsync() => RunTransitionAsync(LoadInitialStateAsync);

    private async Task LoadInitialStateAsync(CancellationToken token)
    {
        logger.Info("Initializing GuildSaber runtime...");

        // Throw if cancellation requested manually because the underlying GetUserInfo API doesn't use the token.
        token.ThrowIfCancellationRequested();

        if (!BeatLeaderId.TryCreate(platform.user.userId)
                .TryGetValue(out var beatLeaderId, out var parseError))
        {
            Publish(new Failed($"Failed to identify the local BeatLeader player: {parseError}"));
            return;
        }

        var playerIdResult = await client.Players.LookupPlayerIdByBeatLeaderIdAsync(beatLeaderId, token);
        if (!playerIdResult.TryGetValue(out var playerId, out var lookupError))
        {
            Publish(new Failed(lookupError));
            return;
        }

        if (playerId is null)
        {
            Publish(new AccountRequired(beatLeaderId));
            return;
        }

        var playerResult = await client.Players.GetExtendedByIdAsync(playerId.Value, token);
        if (!playerResult.TryGetValue(out var player, out var playerError) || player is null)
        {
            Publish(new Failed(playerError ?? "GuildSaber returned no player data."));
            return;
        }

        var guildResults = (await Task.WhenAll(player
                .Members
                .Select(x => new GuildId(x.GuildId)).Distinct()
                .Select(x => client.Guilds.GetExtendedByIdAsync(x, token))))
            .Reduce()
            .Map(x => x.Select(guild => guild ?? throw new ArgumentNullException(nameof(guild))).ToImmutableArray());

        if (!guildResults.TryGetValue(out var guilds, out var error))
        {
            Publish(new Failed(error));
            return;
        }

        _playerExtended = player;
        _availableGuilds = guilds;

        if (_availableGuilds.IsEmpty)
        {
            Publish(new NoGuilds());
            return;
        }

        var selectedGuild = _availableGuilds.FirstOrDefault(x => x.Guild.Id == config.GuildId && x.Contexts.Length > 0)
                            ?? _availableGuilds.FirstOrDefault(x => x.Contexts.Length > 0);
        if (selectedGuild is null)
        {
            Publish(new Failed("None of the player's guilds have a selectable context."));
            return;
        }

        var selectedContextId = selectedGuild.Contexts.Any(x => x.Id == config.ContextId)
            ? config.ContextId
            : selectedGuild.Contexts[0].Id;

        await LoadGuildContextSelectionAsync(selectedGuild.Guild.Id, selectedContextId, token);
    }

    private async Task LoadGuildContextSelectionAsync(GuildId guildId, ContextId contextId, CancellationToken token)
    {
        if (_playerExtended is null)
        {
            Publish(new Failed("The local player has not been loaded."));
            return;
        }

        var guildExtended = _availableGuilds.FirstOrDefault(x => x.Guild.Id == guildId);
        if (guildExtended is null)
        {
            Publish(new Failed($"Guild {guildId.Value} is not available for this player."));
            return;
        }

        if (guildExtended.Contexts.All(x => x.Id != contextId))
        {
            Publish(new Failed($"Context {contextId.Value} does not belong to guild {guildExtended.Guild.Info.Name}."));
            return;
        }

        var stats = await FetchMemberStatsAsync(_playerExtended.Player.Id, contextId, token);
        if (stats is not { } memberStats)
        {
            Publish(new Failed("Failed to load the player's guild statistics."));
            return;
        }

        var snapshot = new GuildSaberSnapshot(
            _playerExtended,
            _availableGuilds,
            guildExtended,
            contextId,
            memberStats.LevelStats,
            memberStats.ContextStats
        );

        config.GuildId = guildId;
        config.ContextId = contextId;

        Publish(new Ready(snapshot));
    }

    public Task SelectGuildAsync(GuildResponses.GuildExtended guild)
    {
        if (guild.Contexts.Length != 0)
            return SelectGuildAsync(guild.Guild.Id, guild.Contexts[0].Id);

        _activeTransition?.Cancel();
        _activeTransition = null;

        Publish(new Failed($"Guild {guild.Guild.Info.Name} has no selectable context.", CanRetry: false));
        return Task.CompletedTask;
    }

    public Task SelectGuildAsync(GuildId guildId, ContextId contextId)
        => RunTransitionAsync(token => LoadGuildContextSelectionAsync(guildId, contextId, token));

    public async Task<bool> RefreshCurrentMemberStatsAsync()
    {
        if (State is not Ready(var readySnapshot))
            return false;

        try
        {
            var stats = await FetchMemberStatsAsync(
                readySnapshot.PlayerId,
                readySnapshot.CurrentContextId,
                _lifetime.Token);

            if (stats is not { } memberStats)
                return false;

            if (State is not Ready(var currentSnapshot)
                || currentSnapshot.PlayerId != readySnapshot.PlayerId
                || currentSnapshot.CurrentGuildExtended.Guild.Id != readySnapshot.CurrentGuildExtended.Guild.Id
                || currentSnapshot.CurrentContextId != readySnapshot.CurrentContextId)
                return false;

            var refreshedSnapshot = currentSnapshot with
            {
                LevelStats = memberStats.LevelStats,
                ContextStats = memberStats.ContextStats
            };

            Publish(new Ready(refreshedSnapshot));
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.Error($"Unexpected error refreshing GuildSaber member stats: {exception}");
            return false;
        }
    }

    private async Task<MemberStats?> FetchMemberStatsAsync(
        PlayerId playerId,
        ContextId contextId,
        CancellationToken token)
    {
        var (levelStatsResult, contextStatsResult) = await (
            client.LevelStats.GetByPlayerIdAsync(playerId, contextId, token),
            client.ContextStats.GetByPlayerIdAsync(playerId, contextId, token)
        ).WhenAll();

        if (!levelStatsResult.TryGetValue(out var levelStats, out var levelStatsError))
        {
            logger.Error($"Failed to fetch member level stats: {levelStatsError}");
            return null;
        }

        // ReSharper disable once InvertIf
        if (!contextStatsResult.TryGetValue(out var contextStats, out var contextStatsError) || contextStats is null)
        {
            logger.Error($"Failed to fetch member context stats: {contextStatsError}");
            return null;
        }

        return new MemberStats([..levelStats], contextStats.Value);
    }

    private async Task RunTransitionAsync(Func<CancellationToken, Task> transition)
    {
        _activeTransition?.Cancel();

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _activeTransition = cancellation;

        Publish(new Loading());

        try
        {
            await transition(cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // A newer transition owns the state now, or the manager is being disposed.
        }
        catch (Exception exception)
        {
            logger.Error($"Unexpected GuildSaber runtime error: {exception}");
            if (!cancellation.IsCancellationRequested)
                Publish(new Failed("An unexpected error occurred while loading GuildSaber."));
        }
        finally
        {
            if (ReferenceEquals(_activeTransition, cancellation)) _activeTransition = null;
        }
    }

    private void Publish(GuildSaberRuntimeState state)
    {
        if (state is Failed(var message, _)) logger.Error(message);
        State = state;
        StateChanged?.Invoke(state);
    }
}