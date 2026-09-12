using System.Text.Json.Serialization;

namespace GuildSaber.Api.Features.Guilds.Achievements.Http;

public static class AchievementResponses
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(Ordered), "Ordered")]
    [JsonDerivedType(typeof(Unordered), "Unordered")]
    public abstract record AchievementProgression
    {
        public sealed record Ordered(
            uint Order,
            bool IsLocking
        ) : AchievementProgression;

        public sealed record Unordered : AchievementProgression
        {
            public Unordered() { }

            // Keeps EF Core from treating this parameterless response variant as a captured projection constant.
            internal Unordered(int _) { }
        }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(RankedMapListAchievement), "RankedMapList")]
    [JsonDerivedType(typeof(DiffStarAchievement), "DiffStar")]
    [JsonDerivedType(typeof(AccStarAchievement), "AccStar")]
    public abstract record Achievement(
        int Id,
        GuildId GuildId,
        ContextId ContextId,
        CategoryId? CategoryId,
        AchievementInfo Info,
        AchievementDiscordBindings DiscordBindings,
        AchievementProgression Progression,
        Xp UnlockXp
    )
    {
        public sealed record RankedMapListAchievement(
            int Id,
            GuildId GuildId,
            ContextId ContextId,
            CategoryId? CategoryId,
            AchievementInfo Info,
            AchievementDiscordBindings DiscordBindings,
            AchievementProgression Progression,
            Xp UnlockXp,
            uint RequiredPassCount,
            int TotalCount
        ) : Achievement(Id, GuildId, ContextId, CategoryId, Info, DiscordBindings, Progression, UnlockXp);

        public sealed record DiffStarAchievement(
            int Id,
            GuildId GuildId,
            ContextId ContextId,
            CategoryId? CategoryId,
            AchievementInfo Info,
            AchievementDiscordBindings DiscordBindings,
            AchievementProgression Progression,
            Xp UnlockXp,
            float MinStar,
            uint RequiredPassCount,
            int TotalCount,
            float? MaxStar = null
        ) : Achievement(Id, GuildId, ContextId, CategoryId, Info, DiscordBindings, Progression, UnlockXp);

        public sealed record AccStarAchievement(
            int Id,
            GuildId GuildId,
            ContextId ContextId,
            CategoryId? CategoryId,
            AchievementInfo Info,
            AchievementDiscordBindings DiscordBindings,
            AchievementProgression Progression,
            Xp UnlockXp,
            float MinStar,
            uint RequiredPassCount,
            int TotalCount,
            float? MaxStar = null
        ) : Achievement(Id, GuildId, ContextId, CategoryId, Info, DiscordBindings, Progression, UnlockXp);
    }

    public record AchievementInfo(
        string Name,
        int Color
    );

    public record AchievementDiscordBindings(DiscordRoleId? RoleId);
}