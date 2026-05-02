using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core.UI.Common;
using IPA.Utilities;
using SiraUtil.Logging;
using UnityEngine;

namespace GuildSaber.Mod.Core.UI.RankedMap;

public class MapRankedStat : XUIHLayout
{
    public static List<MapRankedStat> Shitpoost = new List<MapRankedStat>();
    
    private XUIImage _guildIcon = null!;
    private GSText _mapLevel = null!;

    private readonly ModData _modData;

    private readonly PluginConfig _config = null!;

    private SiraLog _logger = null!;

    public MapRankedStat(ModData modData, UIFactory factory, PluginConfig config, SiraLog logger) : base(
        "MapRankedStats", [])
    {
        _logger = logger;
        
        Shitpoost.Add(this);
        
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

        
        
        _modData = modData;
        _config = config;
    }
    
    
    public async void BeatmapDifficultyChanged(StandardLevelDetailViewController standardLevelDetailView, StandardLevelDetailViewController.ContentType contentType)
    {
        var beatmapKey = standardLevelDetailView.beatmapKey;
        //var beatmalLevel = standardLevelDetailView.GetField<BeatmapLevel, StandardLevelDetailView>("_beatmapLevel");
        var beatmalLevel = standardLevelDetailView.beatmapLevel;
        var hash = SongCore.Utilities.Hashing.ComputeCustomLevelHash(beatmalLevel);

        _logger.Error(hash + " ; ; ; ;C4EST LES HASH UWU");

        if (beatmalLevel == null)
        {
            SetActive(false);
            return;
        }
        
        var rankedMapLevel
            = await _modData.GetRankedMapLevel(hash, beatmapKey.beatmapCharacteristic.serializedName,
                (int)beatmapKey.difficulty, 
                _modData.Guilds.First(x => x.Guild.Id == _config.PlayerCard.GuildId).Contexts[0].Id);

        _logger.Error($"RANKED MAP LEVEL: {rankedMapLevel ?? -8}");
        
        if (rankedMapLevel == null)
        {
            SetActive(false);
            return;
        }

        var guildIcon = await _modData.GetGuildLogo(new GuildId(_config.PlayerCard.GuildId));

        _guildIcon.SetSprite(Sprite.Create(guildIcon, new Rect(0, 0, guildIcon.width, guildIcon.height), Vector2.zero));
        _mapLevel.SetText($"{rankedMapLevel}:0");
        SetActive(true);
    }
}