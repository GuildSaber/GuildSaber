using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Guilds.Members.ContextStats;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Api.Features.Players;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Core.PlayerCard;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Core;

public class ModData
{
    [Inject] protected GuildSaberClient _client = null!;

    public PlayerCardResources _resources = null!;

    protected Dictionary<int, CategoryResponses.Category[]> CategoriesCache = new();

    public List<GuildResponses.Guild> Guilds = [];
    public PlayerResponses.PlayerExtended? Player = null!;
    public LevelStatResponses.MemberLevelStat[] PlayerLevels = null!;
    public ContextStatResponses.SimplePointWithRank[] PlayerPoints = null!;
    public Color CardUsedColor = Color.white;


    public GuildResponses.Guild? GetGuild(int id) => Guilds.FirstOrDefault(x => x.Id.Value == id);


    public Texture2D GetGuildLogo(int id) =>
        //var l_Res = await _client.Guilds.GetExtendedByIdAsync(new GuildId(id));
        _resources.GsWhiteLogoTexture;

    public async Task<CategoryResponses.Category[]> GetAllCategories(GuildId guildId)
    {
        if (CategoriesCache.ContainsKey(guildId))
        {
            var res = CategoriesCache.TryFind(guildId);
            if (res.Value.Length > 0) return res.Value;

            CategoriesCache.Remove(guildId);
        }

        var l_ApiRes = await _client.Categories.GetAllByGuildIdAsync(guildId);
        if (l_ApiRes.TryGetValue(out var value)) return [];

        CategoriesCache.Add(guildId, value);
        return value;
    }
}