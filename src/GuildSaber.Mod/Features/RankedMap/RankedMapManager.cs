using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BeatLeader.API;
using BeatLeader.Models;
using BeatLeader.WebRequests;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Caching;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Helpers;
using SongCore.Utilities;
using Zenject;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapResponses;
using RequestState = BeatLeader.WebRequests.RequestState;

namespace GuildSaber.Mod.Features.RankedMap;

public sealed class RankedMapManager(
    GuildSaberManager guildSaberManager,
    GuildSaberCacheStore cacheStore,
    GuildSaberSession session,
    GuildSaberConfig config,
    GuildSaberClient client,
    StandardLevelDetailViewController levelDetailViewController,
    Logger logger) : IInitializable, IDisposable
{
    private static readonly TimeSpan _rankedMapsCacheDuration = TimeSpan.FromMinutes(15);
    private RankedMapEventData? _currentMapSelection;

    [field: MaybeNull, AllowNull]
    private Func<BeatmapLevel, string> GetCustomHashMethodVersionAgnostic => field ??= typeof(Hashing).GetMethods()
        .Where(m => m.Name is "ComputeCustomLevelHash" or "GetCustomLevelHash" &&
                    m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(BeatmapLevel))
        .OrderBy(m => m.Name == "ComputeCustomLevelHash")
        .First(m => m.ReturnType == typeof(string))
        .ToDelegate<Func<BeatmapLevel, string>>();

    public void Dispose()
    {
        session.CurrentGuildContextChanged -= OnCurrentGuildContextChanged;
        levelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
        levelDetailViewController.didChangeContentEvent -= OnContentChanged;

        try
        {
#pragma warning disable CS0618
            UploadReplayRequest.StateChangedEvent -= OnUploadReplayStateChanged;
#pragma warning restore CS0618
        }
        catch
        {
            // ignored because beatleader might dispose it before we do (false warning in logs if not ignored).
        }
    }

    public void Initialize()
    {
        session.CurrentGuildContextChanged += OnCurrentGuildContextChanged;
        levelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
        levelDetailViewController.didChangeDifficultyBeatmapEvent += OnDifficultyChanged;
        levelDetailViewController.didChangeContentEvent -= OnContentChanged;
        levelDetailViewController.didChangeContentEvent += OnContentChanged;

#pragma warning disable CS0618
        UploadReplayRequest.StateChangedEvent += OnUploadReplayStateChanged;
#pragma warning restore CS0618
    }

    private async void OnUploadReplayStateChanged(
        IWebRequest<ScoreUploadResponse> instance, RequestState state, string? failReason)
    {
        try
        {
            if (state != RequestState.Finished)
                return;

            logger.Debug("Refreshing ranked map data after BeatLeader replay upload...");

            await Task.Delay(5000);
            await RefreshAfterCurrentRankedMapPassAsync();
        }
        catch (Exception exception)
        {
            logger.Error($"Error refreshing ranked map after BeatLeader replay upload: {exception}");
        }
    }

    /// <summary>Fired when a new map selection is available (including changes in difficulty/content).</summary>
    /// <remarks>If the map cannot be resolved to a ranked map, the underlying RankedMapWithScores will be null.</remarks>
    public event Action<RankedMapEventData>? OnMapSelected;

    public void RemoveCachedRankedMaps(ContextId contextId, SongHash hash)
        => cacheStore.Remove(GetRankedMapsCacheKey(contextId, hash));

    private void OnDifficultyChanged(StandardLevelDetailViewController controller)
        => _ = UpdateSelection(controller.beatmapKey, controller.beatmapLevel);

    private void OnContentChanged(
        StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType)
    {
        if (contentType != StandardLevelDetailViewController.ContentType.OwnedAndReady) return;
        _ = UpdateSelection(controller.beatmapKey, controller.beatmapLevel);
    }

    private void OnCurrentGuildContextChanged(GuildResponses.GuildExtended guild, ContextId contextId)
        => _ = UpdateSelection(levelDetailViewController.beatmapKey, levelDetailViewController.beatmapLevel);

    private async Task UpdateSelection(BeatmapKey beatmapKey, BeatmapLevel? beatmap)
    {
        if (!guildSaberManager.Initialized || beatmap == null || !SongHash
                .TryCreate(GetCustomHashMethodVersionAgnostic.Invoke(beatmap))
                .TryGetValue(out var songHash))
        {
            PublishMapSelected(new RankedMapEventData(beatmapKey, SongHash: null, RankedMapWithScores: null));
            return;
        }

        RankedMapWithScores? rankedMapWithScores;
        try
        {
            rankedMapWithScores = await FetchRankedMapWithScoresOfPlayer(
                config.ContextId,
                session.PlayerId,
                songHash,
                beatmapKey.beatmapCharacteristic.serializedName,
                beatmapKey.difficulty.ToEDifficulty()
            );
        }
        catch (Exception exception)
        {
            logger.Error(exception);
            rankedMapWithScores = null;
        }

        PublishMapSelected(new RankedMapEventData(beatmapKey, songHash, rankedMapWithScores));
    }

    /// <summary>Refreshes the cached map score data and member stats after the current map may have changed them.</summary>
    public async Task RefreshAfterCurrentRankedMapPassAsync()
    {
        if (levelDetailViewController == null) return;

        var beatmap = levelDetailViewController.beatmapLevel;
        if (beatmap == null) return;

        var beatmapKey = levelDetailViewController.beatmapKey;
        if (!SongHash.TryCreate(GetCustomHashMethodVersionAgnostic.Invoke(beatmap)).TryGetValue(out var songHash))
        {
            logger.Warn("Failed to create song hash for current map, cannot refresh after map pass.");
            return;
        }

        if (!IsCurrentSelectedRankedMap(beatmapKey, songHash))
            return;

        RemoveCachedRankedMaps(config.ContextId, songHash);
        await guildSaberManager.RefreshCurrentMemberStatsAsync();

        await UpdateSelection(beatmapKey, beatmap);
    }

    private bool IsCurrentSelectedRankedMap(BeatmapKey beatmapKey, SongHash songHash)
        => _currentMapSelection is { RankedMapWithScores: not null } selection
           && selection.BeatmapKey.Equals(beatmapKey)
           && selection.SongHash is { } selectedSongHash
           && selectedSongHash.Equals(songHash);

    private void PublishMapSelected(RankedMapEventData eventData)
    {
        _currentMapSelection = eventData;
        OnMapSelected?.Invoke(eventData);
    }

    private async Task<RankedMapWithScores?> FetchRankedMapWithScoresOfPlayer(
        ContextId contextId, PlayerId playerId, SongHash hash, string mode, EDifficulty difficulty)
        => (await FetchRankedMaps(contextId, playerId, hash))
            .FirstOrDefault(x => x.RankedMap.Versions
                .Any(v => v.Difficulty.GameMode == mode && v.Difficulty.Difficulty == difficulty));

    private Task<RankedMapWithScores[]> FetchRankedMaps(ContextId contextId, PlayerId playerId, SongHash hash)
        => cacheStore.GetOrCreateAsync(
            GetRankedMapsCacheKey(contextId, hash),
            async () =>
            {
                var searchResult = await client.RankedMaps.GetWithScoresAsync(
                    contextId,
                    playerId,
                    new RankedMapRequests.Filters(Search: hash),
                    new PaginatedRequestOptions<RankedMapRequests.ERankedMapSorter>(Page: 1, PageSize: 8)
                );

                return searchResult.TryGetValue(out var mapList) && mapList.TotalCount != 0 ? mapList.Data : [];
            }, _rankedMapsCacheDuration);

    private static string GetRankedMapsCacheKey(ContextId contextId, SongHash hash)
        => $"ranked-maps:{contextId.Value}:{hash}";
}