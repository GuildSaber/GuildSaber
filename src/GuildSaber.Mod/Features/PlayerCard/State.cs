using System.Collections.Immutable;
using System.Linq;
using GuildSaber.Common.Extra;
using GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Helpers;
using UnityEngine;
using static GuildSaber.Api.Features.Guilds.Levels.Http.LevelResponses;

namespace GuildSaber.Mod.Features.PlayerCard;

public abstract record PlayerCardState
{
    public sealed record Loading : PlayerCardState;
    public sealed record Unavailable(string Reason, bool CanRetry) : PlayerCardState;
    public sealed record Summary(PlayerCardSummary Data) : PlayerCardState;
    public sealed record Actions(PlayerCardActions Data) : PlayerCardState;
}

public sealed record PlayerCardSummary(
    string PlayerName,
    Texture2D? Avatar,
    Texture2D? GuildIcon,
    string LevelName,
    string Passes,
    ImmutableArray<PlayerCardPoint> Points,
    TrophiesData Trophies,
    PlayerCardProgress Progress,
    PlayerCardPalette Palette,
    bool ShowProgress)
{
    public static PlayerCardSummary Create(
        GuildSaberSnapshot snapshot,
        PlayerCardConfig config,
        Texture2D? avatar,
        Texture2D? guildIcon,
        bool canCustomize)
    {
        var level = snapshot.LevelStats.GetGlobalLevel();
        var automaticColor = level is null ? Color.white : Color.FromArgb(level.Info.Color);
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
        var categoryLevels = snapshot.CurrentGuildExtended.Categories
            .Select(category => (category, level: snapshot.LevelStats.GetCategoryLevel(category.Id)))
            .Select(x => new PlayerCardCategoryLevel(
                x.category.Info.Name,
                x.level?.Info.Name ?? "None",
                x.level?.Order ?? 0,
                Color.FromArgb(x.level?.Info.Color ?? 0xFFFFFF)))
            .ToImmutableArray();
        var averageCategoryLevel = categoryLevels.IsEmpty ? 0 : categoryLevels.Average(x => x.LevelOrder);
        var averageCategoryLevelColor = GetAverageLevelColor(snapshot, averageCategoryLevel);

        var equilibrium = snapshot
            .LevelStats
            .CalculateSkillEquilibrium(snapshot.CurrentGuildExtended.Categories.Select(x => x.Id)) ?? 100;

        if (double.IsNaN(equilibrium) || double.IsInfinity(equilibrium))
            equilibrium = 100;

        return new PlayerCardSummary(
            PlayerName: snapshot.PlayerExtended.Player.PlayerInfo.Username,
            Avatar: avatar,
            GuildIcon: guildIcon,
            LevelName: level?.Info.Name ?? "Level none",
            Passes: $"{globalPasses.PassCount} passes (#{globalPasses.Rank})",
            Points:
            [
                ..snapshot.ContextStats.SimplePointsWithRank
                    .Where(x => x.CategoryId is null)
                    .Select(x => new PlayerCardPoint($"{x.Points:0.##} {x.Name} (#{x.Rank})", pointColor))
            ],
            Trophies: snapshot.LevelStats.CalculateTrophiesData(),
            Progress: new PlayerCardProgress(
                averageCategoryLevel,
                averageCategoryLevelColor,
                equilibrium,
                categoryLevels),
            Palette: palette,
            ShowProgress: config.CategoryLevelViewEnabled && !categoryLevels.IsEmpty);
    }

    private static Color GetAverageLevelColor(GuildSaberSnapshot snapshot, double average)
    {
        var levels = snapshot.LevelStats
            .Select(x => x.Level)
            .OfType<Level.RankedMapListLevel>()
            .Where(x => x.CategoryId is null)
            .OrderBy(x => x.Order)
            .ToArray();

        var (lower, upper) = (
            levels.LastOrDefault(x => x.Order <= average),
            levels.FirstOrDefault(x => x.Order >= average)
        );

        if (lower is null) return Color.FromArgb(upper?.Info.Color ?? 0xFFFFFF);
        if (upper is null || lower.Order == upper.Order) return Color.FromArgb(lower.Info.Color);

        var blend = (float)((average - lower.Order) / (upper.Order - lower.Order));
        return Color.Lerp(Color.FromArgb(lower.Info.Color), Color.FromArgb(upper.Info.Color), blend);
    }
}

public sealed record PlayerCardActions(
    ImmutableArray<PlayerCardGuild> Guilds,
    ImmutableArray<PlayerCardContext> Contexts,
    ContextId CurrentContextId)
{
    public static PlayerCardActions Create(
        GuildSaberSnapshot snapshot,
        ImmutableDictionary<GuildId, Texture2D> guildIcons)
        => new(
            [
                ..snapshot.AvailableGuilds.Select(guild => new PlayerCardGuild(
                    guild.Guild.Id,
                    guild.Guild.Info.Name,
                    guildIcons.GetValueOrDefault(guild.Guild.Id)))
            ],
            [
                ..snapshot.CurrentGuildExtended.Contexts
                    .Select(context => new PlayerCardContext(context.Id, context.Info.Name))
            ],
            snapshot.CurrentContextId);
}

public readonly record struct PlayerCardGuild(GuildId Id, string Name, Texture2D? Icon);
public readonly record struct PlayerCardContext(ContextId Id, string Name);
public readonly record struct PlayerCardPoint(string Text, Color Color);

public readonly record struct PlayerCardProgress(
    double AverageCategoryLevel,
    Color AverageCategoryLevelColor,
    double Equilibrium,
    ImmutableArray<PlayerCardCategoryLevel> Categories
);

public readonly record struct PlayerCardCategoryLevel(
    string CategoryName,
    string LevelName,
    uint LevelOrder,
    Color Color
);

public abstract record PlayerCardPalette(Color Accent)
{
    public sealed record Solid(Color Color) : PlayerCardPalette(Color);
    public sealed record Gradient(Color Accent, Color Start, Color End) : PlayerCardPalette(Accent);
}

public readonly record struct PlayerCardSettingsState(
    bool Enabled,
    bool ShowProgress,
    bool ShowHandle,
    bool CanCustomize,
    PlayerCardColorMode ColorMode,
    Color MainColor,
    Color GradientStart,
    Color GradientEnd)
{
    public static PlayerCardSettingsState Create(PlayerCardConfig config, bool canCustomize) => new(
        Enabled: config.Enabled,
        ShowProgress: config.CategoryLevelViewEnabled,
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