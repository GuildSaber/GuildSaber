using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Guilds.Members.ContextStats;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.Players;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Core.PlayerCard;
using GuildSaber.Mod.Core.UI.Utils;
using ModestTree;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Zenject;

namespace GuildSaber.Mod.Core;

public class ModData
{
    [Inject] protected readonly GuildSaberClient _client = null!;
    [Inject] protected readonly SiraLog _logger = null!;

    public PlayerCardResources CardResources = null!;

    protected readonly Dictionary<int, CategoryResponses.Category[]> CategoriesCache = new();
    protected readonly Dictionary<GuildId, Texture2D> GuildIconCache = new();
    protected readonly Dictionary<string, RankedMapResponses.RankedMap[]> RankedMapsCache = new();

    public List<GuildResponses.GuildExtended> Guilds = [];
    public PlayerResponses.PlayerExtended? Player = null!;
    public LevelStatResponses.MemberLevelStat[] PlayerLevels = null!;
    public ContextStatResponses.SimplePointWithRank[] PlayerPoints = null!;
    public CategoryResponses.Category[] Categories = null!;
    public Color CardUsedColor = Color.white;

    public GuildResponses.GuildExtended? GetGuild(GuildId id)
        => Guilds.FirstOrDefault(x => x.Guild.Id == id);

    public async Task<RankedMapResponses.RankedMap[]?> GetRankedMap(string hash, ContextId context)
    {
        if (RankedMapsCache.TryGetValue(hash, out var map))
        {
            return map;
        }

        var searchResult = await _client.RankedMaps.GetAsync(context, new RankedMapRequests.Filters(Search: hash),
            new PaginatedRequestOptions<RankedMapRequests.ERankedMapSorter>(1, 40));

        if (!searchResult.TryGetValue(out var mapList))
        {
            return null;
        }

        if (mapList.TotalCount == 0) return null;

        return mapList.Data;
    }

    public async Task<float?> GetRankedMapLevel(string hash, string mode, int difficulty, ContextId contextId)
    {
        var rankedMap = await GetRankedMap(hash, contextId);

        var matchingMaps = rankedMap?.FirstOrDefault(x =>
            x.Versions.Last().Difficulty.Difficulty == (EDifficulty)(difficulty * 2 + 1) 
            && x.Versions.Last().Difficulty.GameMode == mode);

        return matchingMaps?.Rating.DiffStar;
    }

    public async Task<Texture2D> GetGuildLogo(GuildId id)
    {
        if (GuildIconCache.ContainsKey(id))
        {
            var found = GuildIconCache.TryFind(id);
            if (found.HasValue)
                return found.Value;

            GuildIconCache.Remove(id);
        }

        var guildUri = _client.Guilds.GetLogoUrl(id);

        var guildLogo = await TextureUtils.FetchImageFromUrl(guildUri.AbsoluteUri, CardResources);
        if (guildLogo != null)
        {
            GuildIconCache.Add(id, guildLogo);
            return guildLogo;
        }

        return CardResources.GsWhiteLogoTexture;
    }

    public async Task<CategoryResponses.Category[]> GetAllCategories(GuildId guildId)
    {
        if (CategoriesCache.ContainsKey(guildId))
        {
            var res = CategoriesCache.TryFind(guildId);
            if (res.Value.Length > 0) return res.Value;

            CategoriesCache.Remove(guildId);
        }

        var apiRes = await _client.Categories.GetAllByGuildIdAsync(guildId);
        if (apiRes.TryGetValue(out var value)) return [];

        CategoriesCache.Add(guildId, value);
        return value;
    }
}