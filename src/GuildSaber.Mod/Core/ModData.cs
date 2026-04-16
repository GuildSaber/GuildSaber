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
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.PlayerCard;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Core;

public class ModData
{
    [Inject] protected GuildSaberClient _client = null!;
    [Inject] protected PlayerCardResources _resources = null!;

    protected Dictionary<int, CategoryResponses.Category[]> CategoriesCache = new Dictionary<int, CategoryResponses.Category[]>();
    
    public List<GuildResponses.Guild> Guilds = [];
    public PlayerResponses.PlayerExtended? Player = null!;
    public LevelStatResponses.MemberLevelStat[] PlayerLevels = null!;
    public ContextStatResponses.SimplePointWithRank[] PlayerPoints = null!;
    public Color CardUsedColor = Color.white;
    
    public ModData()
    {
    }

    public GuildResponses.Guild? GetGuild(int id)
    {
        var res = Guilds.Where(x => x.Id.Value == id);
        return !res.Any() ? null : res.First();
    }

    public Texture2D GetGuildLogo(int id)
    {
        //var l_Res = await _client.Guilds.GetExtendedByIdAsync(new GuildId(id));
        return _resources.GsWhiteLogoTexture;
    }

    public async Task<CategoryResponses.Category[]> GetAllCategories(int guildId)
    {
        if (CategoriesCache.ContainsKey(guildId))
        {
            var l_Res = CategoriesCache.TryFind(guildId);
            if (l_Res.Value.Length > 0)
            {
                return l_Res.Value;
            }
            
            CategoriesCache.Remove(guildId);
        }
        
        var l_ApiRes = await _client.Categories.GetAllByGuildIdAsync(new GuildId(guildId));
        if (l_ApiRes.IsFailure)
        {
            return new CategoryResponses.Category[] {};
        }
        
        CategoriesCache.Add(guildId, l_ApiRes.Value);
        return l_ApiRes.Value;
    }

}