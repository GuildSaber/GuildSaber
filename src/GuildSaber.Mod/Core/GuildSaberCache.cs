using System.Collections.Generic;
using System.Threading.Tasks;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using UnityEngine;
using static GuildSaber.Api.Features.Guilds.GuildResponses;
using static GuildSaber.Api.Features.Players.PlayerResponses;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.LevelStatResponses;
using static GuildSaber.Api.Features.Guilds.Members.ContextStats.ContextStatResponses;
using static GuildSaber.Api.Features.RankedMaps.RankedMapResponses;

namespace GuildSaber.Mod.Core;

public class GuildSaberCache
{
    public PlayerExtended? PlayerExtended { get; set; }
    public Dictionary<GuildId, GuildExtended> GuildsExtended { get; init; } = [];
    public Dictionary<ContextId, MemberLevelStat[]> MemberLevelStats { get; init; } = [];
    public Dictionary<ContextId, MemberContextStat> MemberContextStats { get; init; } = [];
    public Dictionary<GuildId, Texture2D> GuildIcons { get; init; } = [];
    public Dictionary<int, Texture2D> CategoryIcons { get; init; } = [];
    public Dictionary<(SongHash, ContextId), RankedMap[]> RankedMaps { get; init; } = [];
}

public static class GuildSaberCacheExtensions
{
    extension(GuildSaberCache self)
    {
        public async Task<Texture2D?> FetchGuildIconTexture(GuildId guildId, GuildSaberClient client)
        {
            if (self.GuildIcons.TryGetValue(guildId, out var cachedIcon) && cachedIcon != null)
                return cachedIcon;

            var uri = client.Guilds.GetLogoUrl(guildId);

            var texture = new Texture2D(100, 100);
            try
            {
                var bytes = await client.HttpClient.GetByteArrayAsync(uri);
                texture.LoadImage(bytes, false);
            }
            catch
            {
                return null;
            }

            if (texture == null)
                return null;

            self.GuildIcons[guildId] = texture;
            return texture;
        }
        
        public async Task<Texture2D?> FetchCategoryIconTexture(int categoryId, GuildSaberClient client)
        {
            if (self.CategoryIcons.TryGetValue(categoryId, out var cachedIcon) && cachedIcon != null)
                return cachedIcon;

            var uri = client.Categories.GetLogoUrl(categoryId);

            var texture = new Texture2D(100, 100);
            try
            {
                var bytes = await client.HttpClient.GetByteArrayAsync(uri);
                texture.LoadImage(bytes, false);
            }
            catch
            {
                return null;
            }

            if (texture == null)
                return null;

            self.CategoryIcons[categoryId] = texture;
            return texture;
        }

        public async Task<RankedMap[]> FetchRankedMaps(ContextId contextId, SongHash hash, GuildSaberClient client)
        {
            if (self.RankedMaps.TryGetValue((hash, contextId), out var rankedMap)) return rankedMap;

            var searchResult = await client.RankedMaps.GetAsync(
                contextId,
                new RankedMapRequests.Filters(Search: hash),
                new PaginatedRequestOptions<RankedMapRequests.ERankedMapSorter>(Page: 1, PageSize: 8)
            );

            if (!searchResult.TryGetValue(out var mapList) || mapList.TotalCount == 0)
                return self.RankedMaps[(hash, contextId)] = [];

            return self.RankedMaps[(hash, contextId)] = mapList.Data;
        }
    }
}