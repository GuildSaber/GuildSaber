using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GuildSaber.Api.Features.Guilds.Achievements.Http;

public static class AchievementRequests
{
    /// <summary>Complete fields used to create or update an achievement within a context.</summary>
    /// <param name="Name">Display name, between 1 and 32 characters.</param>
    /// <param name="Color">Display color as a signed ARGB integer.</param>
    /// <param name="CategoryId">Category from the achievement's guild. Null makes the achievement global.</param>
    /// <param name="RoleId">Optional Discord role associated with the achievement.</param>
    /// <param name="ProgressionOrder">Position in the level progression. Null excludes it from progression.</param>
    /// <param name="IsLocking">Whether this ordered achievement locks later achievements until completed.</param>
    /// <param name="UnlockXp">
    /// Reserved XP unlock setting. Current completion rules use pass counts and progression.
    /// </param>
    /// <param name="Requirement">Completion rule. An existing achievement must retain its rule type.</param>
    public sealed record Definition(
        [property: Required, StringLength(32, MinimumLength = 1)]
        string Name,
        int Color,
        CategoryId? CategoryId,
        DiscordRoleId? RoleId,
        uint? ProgressionOrder,
        bool IsLocking,
        Xp UnlockXp,
        Requirement Requirement);

    /// <summary>Completion rule for an achievement.</summary>
    /// <param name="RequiredPassCount">Number of qualifying ranked-map passes needed for completion.</param>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(RankedMapList), "RankedMapList")]
    [JsonDerivedType(typeof(DiffStar), "DiffStar")]
    [JsonDerivedType(typeof(AccStar), "AccStar")]
    public closed record Requirement(uint RequiredPassCount)
    {
        /// <summary>Complete the required number of explicitly associated ranked maps.</summary>
        public sealed record RankedMapList(uint RequiredPassCount) : Requirement(RequiredPassCount);

        /// <summary>Complete maps with difficulty stars in the given range.</summary>
        /// <param name="MinStar">Inclusive lower bound.</param>
        /// <param name="RequiredPassCount">Number of qualifying passes needed for completion.</param>
        /// <param name="MaxStar">Exclusive upper bound. Null or omitted leaves the range unbounded.</param>
        public sealed record DiffStar(
            [property: Range(0, float.MaxValue)]
            float MinStar,
            uint RequiredPassCount,
            [property: Range(0, float.MaxValue)]
            float? MaxStar = null) : Requirement(RequiredPassCount);

        /// <summary>Complete maps with accuracy stars in the given range.</summary>
        /// <param name="MinStar">Inclusive lower bound.</param>
        /// <param name="RequiredPassCount">Number of qualifying passes needed for completion.</param>
        /// <param name="MaxStar">Exclusive upper bound. Null or omitted leaves the range unbounded.</param>
        public sealed record AccStar(
            [property: Range(0, float.MaxValue)]
            float MinStar,
            uint RequiredPassCount,
            [property: Range(0, float.MaxValue)]
            float? MaxStar = null) : Requirement(RequiredPassCount);
    }
}