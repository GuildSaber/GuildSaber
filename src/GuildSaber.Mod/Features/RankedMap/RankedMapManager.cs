using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Helpers;
using SongCore.Utilities;
using Zenject;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapResponses;

namespace GuildSaber.Mod.Features.RankedMap;

public sealed class RankedMapManager(
    GuildSaberCache cache,
    GuildSaberConfig config,
    GuildSaberClient client,
    StandardLevelDetailViewController levelDetailViewController,
    Logger logger)
    : IInitializable, IDisposable
{
    [field: MaybeNull, AllowNull]
    private Func<BeatmapLevel, string> GetCustomHashMethodVersionAgnostic => field ??= typeof(Hashing).GetMethods()
        .Where(m => m.Name is "ComputeCustomLevelHash" or "GetCustomLevelHash" &&
                    m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(BeatmapLevel))
        .OrderBy(m => m.Name == "ComputeCustomLevelHash")
        .First(m => m.ReturnType == typeof(string))
        .ToDelegate<Func<BeatmapLevel, string>>();

    public void Dispose()
    {
        levelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
        levelDetailViewController.didChangeContentEvent -= OnContentChanged;
    }

    public void Initialize()
    {
        levelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
        levelDetailViewController.didChangeDifficultyBeatmapEvent += OnDifficultyChanged;
        levelDetailViewController.didChangeContentEvent -= OnContentChanged;
        levelDetailViewController.didChangeContentEvent += OnContentChanged;
    }

    /// <summary>Fired when a new map selection is available (including changes in difficulty/content).</summary>
    /// <remarks>If the map cannot be resolved to a ranked map, the underlying RankedMapWithScores will be null.</remarks>
    public event Action<RankedMapEventData>? OnMapSelected;

    private void OnDifficultyChanged(StandardLevelDetailViewController controller)
        => _ = UpdateSelection(controller.beatmapKey, controller.beatmapLevel);

    private void OnContentChanged(
        StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType)
    {
        if (contentType != StandardLevelDetailViewController.ContentType.OwnedAndReady) return;
        _ = UpdateSelection(controller.beatmapKey, controller.beatmapLevel);
    }

    private async Task UpdateSelection(BeatmapKey beatmapKey, BeatmapLevel? beatmap)
    {
        if (cache.PlayerExtended == null || beatmap == null || !SongHash
                .TryCreate(GetCustomHashMethodVersionAgnostic.Invoke(beatmap))
                .TryGetValue(out var songHash))
        {
            PublishSelection(beatmapKey, songHash: null, rankedMapWithScores: null);
            return;
        }

        RankedMapWithScores? rankedMapWithScores;
        try
        {
            rankedMapWithScores = await FetchRankedMapWithScoresOfPlayer(
                config.ContextId,
                cache.PlayerExtended!.Player.Id,
                songHash,
                beatmapKey.beatmapCharacteristic.serializedName,
                beatmapKey.difficulty.ToEDifficulty()
            );
        }
        catch (Exception exception)
        {
            logger.Error(exception);
            PublishSelection(beatmapKey, songHash, rankedMapWithScores: null);
            return;
        }

        PublishSelection(beatmapKey, songHash, rankedMapWithScores);
    }

    private void PublishSelection(BeatmapKey beatmapKey, SongHash? songHash, RankedMapWithScores? rankedMapWithScores)
        => OnMapSelected?.Invoke(new RankedMapEventData(beatmapKey, songHash, rankedMapWithScores));

    private async Task<RankedMapWithScores?> FetchRankedMapWithScoresOfPlayer(
        ContextId contextId, PlayerId playerId, SongHash hash, string mode, EDifficulty difficulty)
        => (await cache.FetchRankedMaps(contextId, playerId, hash, client))
            .FirstOrDefault(x => x.RankedMap.Versions
                .Any(v => v.Difficulty.GameMode == mode && v.Difficulty.Difficulty == difficulty));
}