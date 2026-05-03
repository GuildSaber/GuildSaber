using System.Linq;
using System.Threading.Tasks;
using CP_SDK.XUI;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core.UI.Common;
using GuildSaber.Mod.Core.UI.Extensions;
using GuildSaber.Mod.Resources;
using SiraUtil.Logging;
using SongCore.Utilities;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Core.UI.RankedMap;

public class MapRankedStat : XUIHLayout
{
    private readonly GuildSaberClient _client;
    private readonly PluginConfig _config;
    private readonly Texture2D _gsWhiteLogoTexture;

    private readonly GuildSaberCache _guildSaberCache;
    private readonly SiraLog _logger;

    private XUIImage _guildIcon = null!;
    private GSText _mapLevel = null!;

    public MapRankedStat(
        GuildSaberCache guildSaberCache, UIFactory factory, PluginConfig config, SiraLog logger,
        GuildSaberClient client,
        [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D gsWhiteLogoTexture) : base("MapRankedStats")
    {
        _logger = logger;
        _guildSaberCache = guildSaberCache;
        _config = config;
        _client = client;
        _gsWhiteLogoTexture = gsWhiteLogoTexture;

        OnReady(x =>
        {
            XUIImage.Make()
                .SetHeight(7)
                .SetWidth(7)
                .Bind(ref _guildIcon)
                .BuildUI(Element.transform);

            factory.Text(string.Empty)
                .Bind(ref _mapLevel)
                .BuildUI(Element.transform);

            x.HLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        });
    }


    public async void BeatmapDifficultyChanged(
        StandardLevelDetailViewController standardLevelDetailView,
        StandardLevelDetailViewController.ContentType contentType)
    {
        var beatmapKey = standardLevelDetailView.beatmapKey;
        var beatmapLevel = standardLevelDetailView.beatmapLevel;
        var hash = Hashing.ComputeCustomLevelHash(beatmapLevel);

        _logger.Error(
            $"{hash} ; ; ; ;C4EST LES HASH UWU, {beatmapKey.beatmapCharacteristic.serializedName} ; {beatmapKey.difficulty.ToEDifficulty()}");

        if (beatmapLevel == null)
        {
            SetActive(false);
            return;
        }

        var rankedMapLevel = await FetchRankedMapLevel(
            _config.PlayerCard.ContextId,
            SongHash.CreateUnsafe(hash).Value,
            beatmapKey.beatmapCharacteristic.serializedName,
            beatmapKey.difficulty.ToEDifficulty(),
            _client
        );

        _logger.Error($"RANKED MAP LEVEL: {rankedMapLevel ?? -8}");

        if (rankedMapLevel == null)
        {
            SetActive(false);
            return;
        }

        _logger.Error($"FETCHING GUILD ICON FOR GUILD ID: {_config.PlayerCard.GuildId}");

        var guildIconTexture = await _guildSaberCache.FetchGuildIconTexture(_config.PlayerCard.GuildId, _client)
                               ?? _gsWhiteLogoTexture;

        _logger.Error($"GUILD ICON TEXTURE: {guildIconTexture.width}");
        _guildIcon.SetSprite(Sprite.Create(
            guildIconTexture,
            new Rect(0, 0, guildIconTexture.width, guildIconTexture.height),
            Vector2.zero)
        );
        _logger.Error($"SETTING MAP LEVEL TEXT: {rankedMapLevel}:0");
        _mapLevel.SetText($"{rankedMapLevel}:0");
        SetActive(true);
    }

    public async Task<float?> FetchRankedMapLevel(
        ContextId contextId, SongHash hash, string mode, EDifficulty difficulty, GuildSaberClient client)
        => (await _guildSaberCache.FetchRankedMaps(contextId, hash, client))
            .FirstOrDefault(x => x
                .Versions
                .Any(v => v.Difficulty.GameMode == mode && v.Difficulty.Difficulty == difficulty))
            ?.Rating.DiffStar;
}