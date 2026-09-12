using System.Collections.Immutable;
using System.Linq;
using GuildSaber.Common.Extra;
using GuildSaber.CSharpClient.Routes.Guilds.Members.AchievementStats;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Helpers;
using UnityEngine;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.Mod.Features.PlayerCard;

public abstract record PlayerCardState
{
    public sealed record Loading : PlayerCardState;
    public sealed record Unavailable(string Reason, bool CanRetry) : PlayerCardState;
    public sealed record Ready(PlayerCardReady Data) : PlayerCardState;
    public sealed record Actions(PlayerCardActionData Data) : PlayerCardState;
}

public sealed record PlayerCardReady(
    string PlayerName,
    Texture2D? Avatar,
    Texture2D? GuildIcon,
    string AchievementName,
    string Passes,
    ImmutableArray<PlayerCardPoint> Points,
    TrophiesData Trophies,
    PlayerCardProgress Progress,
    PlayerCardPalette Palette,
    bool ShowOrderedAchievements)
{
    public static PlayerCardReady Create(
        GuildSaberSnapshot snapshot,
        PlayerCardConfig config,
        Texture2D? avatar,
        Texture2D? guildIcon,
        bool canCustomize)
    {
        var achievement = snapshot.AchievementStats.GetGlobalAchievement();
        var automaticColor = achievement is null ? Color.white : Color.FromArgb(achievement.Info.Color);
        var colors = config.ColorSettings;
        PlayerCardPalette palette = (canCustomize, config.ColorMode) switch
        {
            (canCustomize: true, PlayerCardColorMode.Solid) => new PlayerCardPalette.Solid(colors.MainCardColor),
            (canCustomize: true, PlayerCardColorMode.Gradient) => new PlayerCardPalette.Gradient(
                colors.MainCardColor,
                colors.GradientColor0,
                colors.GradientColor1),
            _ => new PlayerCardPalette.Solid(automaticColor)
        };

        var globalPasses = snapshot.ContextStats.PassCountsWithRank.FirstOrDefault(x => x.CategoryId is null);
        var pointColor = canCustomize && config.ColorMode is not PlayerCardColorMode.Automatic
            ? palette.Accent
            : Color.white;
        var categoryAchievements = snapshot.CurrentGuildExtended.Categories
            .Select(category => (category,
                achievement: snapshot.AchievementStats.GetCategoryAchievement(category.Id)))
            .Select(x => new PlayerCardCategoryAchievement(
                x.category.Info.Name,
                x.achievement?.Info.Name ?? "None",
                x.achievement?.Progression is AchievementProgression.Ordered progression
                    ? progression.Order
                    : 0,
                Color.FromArgb(x.achievement?.Info.Color ?? 0xFFFFFF)))
            .ToImmutableArray();
        var averageCategoryAchievement = categoryAchievements.IsEmpty
            ? 0
            : categoryAchievements.Average(x => x.AchievementOrder);
        var averageCategoryAchievementColor = GetAverageAchievementColor(snapshot, averageCategoryAchievement);

        var equilibrium = snapshot
            .AchievementStats
            .CalculateSkillEquilibrium(snapshot.CurrentGuildExtended.Categories.Select(x => x.Id)) ?? 100;

        if (double.IsNaN(equilibrium) || double.IsInfinity(equilibrium))
            equilibrium = 100;

        return new PlayerCardReady(
            PlayerName: snapshot.PlayerExtended.Player.PlayerInfo.Username,
            Avatar: avatar,
            GuildIcon: guildIcon,
            AchievementName: achievement?.Info.Name ?? "Achievement none",
            Passes: $"{globalPasses.PassCount} passes (#{globalPasses.Rank})",
            Points:
            [
                .. snapshot.ContextStats.SimplePointsWithRank
                    .Where(x => x.CategoryId is null)
                    .Select(x => new PlayerCardPoint($"{x.Points:0.##} {x.Name} (#{x.Rank})", pointColor))
            ],
            Trophies: snapshot.AchievementStats.CalculateTrophiesData(),
            Progress: new PlayerCardProgress(
                averageCategoryAchievement,
                averageCategoryAchievementColor,
                equilibrium,
                categoryAchievements),
            Palette: palette,
            ShowOrderedAchievements: config.ShowOrderedAchievements && !categoryAchievements.IsEmpty);
    }

    private static Color GetAverageAchievementColor(GuildSaberSnapshot snapshot, double average)
    {
        var achievements = snapshot.AchievementStats
            .Select(x => x.Achievement)
            .Where(x => x.CategoryId is null && x.Progression is AchievementProgression.Ordered)
            .OrderBy(x => ((AchievementProgression.Ordered)x.Progression).Order)
            .ToArray();

        var (lower, upper) = (
            achievements.LastOrDefault(x => ((AchievementProgression.Ordered)x.Progression).Order <= average),
            achievements.FirstOrDefault(x => ((AchievementProgression.Ordered)x.Progression).Order >= average)
        );

        if (lower is null) return Color.FromArgb(upper?.Info.Color ?? 0xFFFFFF);
        var lowerOrder = ((AchievementProgression.Ordered)lower.Progression).Order;
        if (upper is null) return Color.FromArgb(lower.Info.Color);

        var upperOrder = ((AchievementProgression.Ordered)upper.Progression).Order;
        if (lowerOrder == upperOrder) return Color.FromArgb(lower.Info.Color);

        var blend = (float)((average - lowerOrder) / (upperOrder - lowerOrder));
        return Color.Lerp(Color.FromArgb(lower.Info.Color), Color.FromArgb(upper.Info.Color), blend);
    }
}

public sealed record PlayerCardActionData(
    ImmutableArray<PlayerCardGuild> Guilds,
    ImmutableArray<PlayerCardContext> Contexts,
    ContextId CurrentContextId)
{
    public static PlayerCardActionData Create(
        GuildSaberSnapshot snapshot,
        ImmutableDictionary<GuildId, Texture2D> guildIcons)
        => new(
            [
                .. snapshot.AvailableGuilds.Select(guild => new PlayerCardGuild(
                    guild.Guild.Id,
                    guild.Guild.Info.Name,
                    guildIcons.GetValueOrDefault(guild.Guild.Id)))
            ],
            [
                .. snapshot.CurrentGuildExtended.Contexts
                    .Select(context => new PlayerCardContext(context.Id, context.Info.Name))
            ],
            snapshot.CurrentContextId);
}

public readonly record struct PlayerCardGuild(GuildId Id, string Name, Texture2D? Icon);
public readonly record struct PlayerCardContext(ContextId Id, string Name);
public readonly record struct PlayerCardPoint(string Text, Color Color);

public readonly record struct PlayerCardProgress(
    double AverageCategoryAchievement,
    Color AverageCategoryAchievementColor,
    double Equilibrium,
    ImmutableArray<PlayerCardCategoryAchievement> Categories
);

public readonly record struct PlayerCardCategoryAchievement(
    string CategoryName,
    string AchievementName,
    uint AchievementOrder,
    Color Color
);

public abstract record PlayerCardPalette(Color Accent)
{
    public sealed record Solid(Color Color) : PlayerCardPalette(Color);
    public sealed record Gradient(Color Accent, Color Start, Color End) : PlayerCardPalette(Accent);
}

public readonly record struct PlayerCardSettingsState(
    bool Enabled,
    bool ShowOrderedAchievements,
    bool ShowHandle,
    bool CanCustomize,
    PlayerCardColorMode ColorMode,
    Color MainColor,
    Color GradientStart,
    Color GradientEnd)
{
    public static PlayerCardSettingsState Create(PlayerCardConfig config, bool canCustomize) => new(
        Enabled: config.Enabled,
        ShowOrderedAchievements: config.ShowOrderedAchievements,
        ShowHandle: config.ShowHandle,
        CanCustomize: canCustomize,
        ColorMode: config.ColorMode,
        MainColor: config.ColorSettings.MainCardColor,
        GradientStart: config.ColorSettings.GradientColor0,
        GradientEnd: config.ColorSettings.GradientColor1
    );
}

public enum PlayerCardPlacement
{
    Inactive,
    Menu,
    GameplayRunning,
    GameplayPaused
}
