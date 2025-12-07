using System.Text.Json.Serialization;

namespace GuildSaber.Api.Features.Guilds.Members.LevelStats;

public static class LevelStatResponses
{
    public readonly record struct MemberLevelStat(
        Level Level,
        bool IsCompleted,
        bool IsLocked,
        int? PassCount
    );

    public readonly record struct LevelInfo(
        string Name,
        int Color
    );

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(RankedMapListLevel), "RankedMapList")]
    [JsonDerivedType(typeof(DiffStarLevel), "DiffStar")]
    [JsonDerivedType(typeof(AccStarLevel), "AccStar")]
    public abstract record Level(
        int Id,
        int? CategoryId,
        LevelInfo Info,
        int Order,
        bool IsLocking
    )
    {
        public sealed record RankedMapListLevel(
            int Id,
            int? CategoryId,
            LevelInfo Info,
            int Order,
            bool IsLocking,
            int RequiredPassCount,
            int RankedMapCount
        ) : Level(Id, CategoryId, Info, Order, IsLocking);

        public sealed record DiffStarLevel(
            int Id,
            int? CategoryId,
            LevelInfo Info,
            int Order,
            bool IsLocking,
            float? MinDiffStar,
            int RequiredPassCount
        ) : Level(Id, CategoryId, Info, Order, IsLocking);

        public sealed record AccStarLevel(
            int Id,
            int? CategoryId,
            LevelInfo Info,
            int Order,
            bool IsLocking,
            float? MinAccStar,
            int RequiredPassCount
        ) : Level(Id, CategoryId, Info, Order, IsLocking);
    }
}