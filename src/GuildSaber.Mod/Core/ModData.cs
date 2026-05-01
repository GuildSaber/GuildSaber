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
    [Inject] protected readonly GuildSaberClient Client = null!;

    public PlayerCardResources CardResources = null!;

    protected readonly Dictionary<int, CategoryResponses.Category[]> CategoriesCache = new();

    public List<GuildResponses.GuildExtended> Guilds = [];
    public PlayerResponses.PlayerExtended? Player = null!;
    public LevelStatResponses.MemberLevelStat[] PlayerLevels = null!;
    public ContextStatResponses.SimplePointWithRank[] PlayerPoints = null!;
    public Color CardUsedColor = Color.white;

    public GuildResponses.GuildExtended? GetGuild(GuildId id)
        => Guilds.FirstOrDefault(x => x.Guild.Id == id);

    public Texture2D GetGuildLogo(int id) =>
        //var l_Res = await _client.Guilds.GetExtendedByIdAsync(new GuildId(id));
        CardResources.GsWhiteLogoTexture;

    public async Task<CategoryResponses.Category[]> GetAllCategories(GuildId guildId)
    {
        if (CategoriesCache.ContainsKey(guildId))
        {
            var res = CategoriesCache.TryFind(guildId);
            if (res.Value.Length > 0) return res.Value;

            CategoriesCache.Remove(guildId);
        }

        var apiRes = await Client.Categories.GetAllByGuildIdAsync(guildId);
        if (apiRes.TryGetValue(out var value)) return [];

        CategoriesCache.Add(guildId, value);
        return value;
    }
}