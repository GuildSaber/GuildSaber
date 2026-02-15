using System.Text.Json.Serialization;
using GuildSaber.Common.StrongTypes;

namespace GuildSaber.Api.Features.Guilds.Levels;

public static class LevelResponses
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(RankedMapListLevel), "RankedMapList")]
    [JsonDerivedType(typeof(DiffStarLevel), "DiffStar")]
    [JsonDerivedType(typeof(AccStarLevel), "AccStar")]
    public abstract record Level(
        int Id,
        GuildId GuildId,
        ContextId ContextId,
        int? CategoryId,
        LevelInfo Info,
        LevelDiscordInfo DiscordInfo,
        uint Order,
        bool IsLocking
    )
    {
        public sealed record RankedMapListLevel(
            int Id,
            GuildId GuildId,
            ContextId ContextId,
            int? CategoryId,
            LevelInfo Info,
            LevelDiscordInfo DiscordInfo,
            uint Order,
            bool IsLocking,
            uint RequiredPassCount,
            int TotalCount
        ) : Level(Id, GuildId, ContextId, CategoryId, Info, DiscordInfo, Order, IsLocking);

        public sealed record DiffStarLevel(
            int Id,
            GuildId GuildId,
            ContextId ContextId,
            int? CategoryId,
            LevelInfo Info,
            LevelDiscordInfo DiscordInfo,
            uint Order,
            bool IsLocking,
            float MinStar,
            uint RequiredPassCount
        ) : Level(Id, GuildId, ContextId, CategoryId, Info, DiscordInfo, Order, IsLocking);

        public sealed record AccStarLevel(
            int Id,
            GuildId GuildId,
            ContextId ContextId,
            int? CategoryId,
            LevelInfo Info,
            LevelDiscordInfo DiscordInfo,
            uint Order,
            bool IsLocking,
            float MinStar,
            uint RequiredPassCount
        ) : Level(Id, GuildId, ContextId, CategoryId, Info, DiscordInfo, Order, IsLocking);
    }

    public record LevelInfo(
        string Name,
        int Color
    );

    public record LevelDiscordInfo(DiscordRoleId? RoleId);
}