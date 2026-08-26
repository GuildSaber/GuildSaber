using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using GuildSaber.Common.Helpers;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;

namespace GuildSaber.Api.Features.Website.LinkPreviews;

internal static class RankedMapLinkPreviewRenderer
{
    private const int MaximumTitleLength = 200;
    private const int MaximumDescriptionLength = 500;

    public static string Render(RankedMapLinkPreview preview, string pageUrl)
    {
        var description = BuildDescription(preview).Truncate(MaximumDescriptionLength);
        var themeColor = GetDifficultyColor(preview.Difficulty);

        var encodedTitle = Encode(BuildTitle(preview).Truncate(MaximumTitleLength));
        var encodedDescription = Encode(description);
        var encodedPageUrl = Encode(pageUrl);
        var encodedCoverUrl = Encode($"https://eu.cdn.beatsaver.com/{preview.Hash}.jpg");
        var encodedImageAlt = Encode($"Cover art for {preview.SongInfo.SongName}");
        var encodedBodyDescription = string.Join("<br>", description.Split('\n').Select(Encode));

        return $$"""
                 <!doctype html>
                 <html lang="en">
                 <head>
                   <meta charset="utf-8">
                   <meta name="viewport" content="width=device-width, initial-scale=1">
                   <title>{{encodedTitle}} | GuildSaber</title>
                   <meta name="description" content="{{encodedDescription}}">
                   <meta name="theme-color" content="{{themeColor}}">
                   <link rel="canonical" href="{{encodedPageUrl}}">
                   <meta property="og:type" content="website">
                   <meta property="og:site_name" content="GuildSaber">
                   <meta property="og:title" content="{{encodedTitle}}">
                   <meta property="og:description" content="{{encodedDescription}}">
                   <meta property="og:url" content="{{encodedPageUrl}}">
                   <meta property="og:image" content="{{encodedCoverUrl}}">
                   <meta property="og:image:secure_url" content="{{encodedCoverUrl}}">
                   <meta property="og:image:type" content="image/jpeg">
                   <meta property="og:image:width" content="512">
                   <meta property="og:image:height" content="512">
                   <meta property="og:image:alt" content="{{encodedImageAlt}}">
                   <meta name="twitter:card" content="summary">
                   <meta name="twitter:title" content="{{encodedTitle}}">
                   <meta name="twitter:description" content="{{encodedDescription}}">
                   <meta name="twitter:image" content="{{encodedCoverUrl}}">
                   <meta name="twitter:image:alt" content="{{encodedImageAlt}}">
                 </head>
                 <body>
                   <main>
                     <h1><a href="{{encodedPageUrl}}">{{encodedTitle}}</a></h1>
                     <img src="{{encodedCoverUrl}}" alt="{{encodedImageAlt}}" width="512" height="512">
                     <p>{{encodedBodyDescription}}</p>
                   </main>
                 </body>
                 </html>
                 """;
    }

    private static string BuildTitle(RankedMapLinkPreview preview)
    {
        var title = string.IsNullOrWhiteSpace(preview.SongInfo.BeatSaverName)
            ? preview.SongInfo.SongName
            : preview.SongInfo.BeatSaverName;

        return preview.BeatSaverKey is { } beatSaverKey
            ? $"{title} ({beatSaverKey.ToBsrKey()})"
            : title;
    }

    private static string BuildDescription(RankedMapLinkPreview preview)
    {
        var description = new StringBuilder()
            .Append("Mapper(s): ").AppendLine(preview.SongInfo.MapperName)
            .Append("Difficulty: ").Append(GetDifficultyName(preview.Difficulty)).Append(", ")
            .AppendLine(FormatGameMode(preview.GameMode))
            .AppendLine()
            .Append("⭐: ").Append(FormatNumber(preview.Rating.DiffStar, "0.00"))
            .Append(" | ✨: ").Append(FormatNumber(preview.Rating.AccStar, "0.00"));

        if (preview.Requirements.MinAccuracy is { } minimumAccuracy)
            description.Append(" (Acc > ").Append(FormatNumber(minimumAccuracy, "0.##")).Append("%)");

        if (preview.Categories.Length > 0)
            description.AppendLine().Append("Categories: ")
                .Append(string.Join(", ", preview.Categories.Select(category => category.ToString())));

        description.AppendLine()
            .Append("NJS: ").Append(FormatNumber(preview.DifficultyStats.NoteJumpSpeed, "0.##"))
            .Append(" | NPS: ").Append(FormatNumber(preview.DifficultyStats.NotesPerSecond, "0.00"))
            .Append(" | Length: ").Append(FormatDuration(preview.SongStats.DurationSec))
            .Append(" | BPM: ").Append(FormatNumber(preview.SongStats.BPM, "0.##"));

        if (preview.Requirements.ProhibitedModifiers is not
            (AbstractScore.EModifiers.None or AbstractScore.EModifiers.ProhibitedDefaults))
            description.AppendLine().Append("Prohibited Modifiers: ")
                .Append(FormatModifiers(preview.Requirements.ProhibitedModifiers));

        if (preview.Requirements.MandatoryModifiers is not AbstractScore.EModifiers.None)
            description.AppendLine().Append("Mandatory Modifiers: ")
                .Append(FormatModifiers(preview.Requirements.MandatoryModifiers));

        var requirements = GetRequirements(preview.Requirements);
        if (requirements.Length > 0)
            description.AppendLine().Append("Requirements: ").Append(string.Join(" · ", requirements));

        return description.ToString();
    }

    private static string[] GetRequirements(RankedMapRequirements requirements)
    {
        List<string> values = [];

        if (requirements.NeedFullCombo)
            values.Add("Full Combo");
        if (requirements.MaxPauseDurationSec is { } pauseDuration)
            values.Add($"Pause < {FormatNumber(pauseDuration, "0.##")}s");
        if (requirements.NeedConfirmation)
            values.Add("Confirmation");

        return values.ToArray();
    }

    private static string GetDifficultyName(EDifficulty difficulty) => difficulty switch
    {
        EDifficulty.ExpertPlus => "Expert+",
        _ => difficulty.ToString()
    };

    private static string GetDifficultyColor(EDifficulty difficulty) => difficulty switch
    {
        EDifficulty.Easy => "#3cb371",
        EDifficulty.Normal => "#59b0f4",
        EDifficulty.Hard => "#ee5e44",
        EDifficulty.Expert => "#bf2a42",
        EDifficulty.ExpertPlus => "#8f48db",
        _ => "#ffffff"
    };

    private static string FormatGameMode(string gameMode) => gameMode switch
    {
        "OneSaber" => "One Saber",
        "NoArrows" => "No Arrows",
        "90Degree" => "90°",
        "360Degree" => "360°",
        _ => gameMode
    };

    private static string FormatModifiers(AbstractScore.EModifiers modifiers)
        => modifiers.ToString().Replace(", ", " + ", StringComparison.Ordinal);

    private static string FormatDuration(float seconds)
    {
        var duration = TimeSpan.FromSeconds(seconds);
        return duration.Hours > 0
            ? duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
            : duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(float value, string format)
        => value.ToString(format, CultureInfo.InvariantCulture);

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
}