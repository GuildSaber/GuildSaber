using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Result;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.AutocompleteHandlers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Commands.ScoringTeam;

public partial class ScoringTeamModuleSlash
{
    [SlashCommand("scoring-team-menu", "Open the scoring team menu")]
    public async Task ScoringTeamMenu([Autocomplete<ContextAutocompleteHandler>] ContextId contextId)
    {
        await DeferAsync(ephemeral: true);

        var guildId = await GetGuildIdAsync();
        var categories = await Cache.GetGuildCategoriesAsync(guildId, Client.Value);

        var filter = new RankedScoreRequests.Filters(RankedScoreTypes: RankedScoreRequests.ERankedScoreType.Pending);
        var sorter = new PaginatedRequestOptions<RankedScoreRequests.ERankedScoreSorter>
        {
            Page = 1,
            PageSize = 2,
            SortBy = RankedScoreRequests.ERankedScoreSorter.ScoreTime,
            Order = EOrder.Asc
        };

        var pendingScores = await Client.Value.RankedScores
            .GetAsync(contextId, filter, sorter)
            .Unwrap();

        var component = ScoringTeamMenuView.BuildComponent(pendingScores.Data, categories, EmojiSettings).Build();
        await FollowupAsync(components: component);
    }
}

file static class ScoringTeamMenuView
{
    public static ComponentBuilderV2 BuildComponent(
        RankedScoreResponses.RankedScoreWithRankedMap[] pendingScores,
        CategoryResponses.Category[] categories,
        IOptions<EmojiSettings> emojiSettings)
    {
        var builder = new ComponentBuilderV2();

        if (pendingScores.Length == 0)
            return builder.WithTextDisplay("No pending scores found.");

        foreach (var pendingScore in pendingScores)
            builder.WithContainer(BuildContainer(pendingScore, categories, emojiSettings));

        return builder;
    }

    private static ContainerBuilder BuildContainer(
        RankedScoreResponses.RankedScoreWithRankedMap data,
        CategoryResponses.Category[] categories,
        IOptions<EmojiSettings> emojiSettings)
    {
        var sectionBuilder = new SectionBuilder();
        var mapContainerBuilder = new ContainerBuilder();

        var rankedMap = data.RankedMap;
        var rankedScore = data.RankedScore;
        foreach (var (i, version) in rankedMap.Versions.Index())
        {
            var song = version.Song;
            if (i > 0) mapContainerBuilder.WithSeparator();
            else mapContainerBuilder.WithAccentColor(Color.FromDifficulty(version.Difficulty.Difficulty));

            var sb = new StringBuilder()
                .Append("**[").Append(song.Info.BeatSaverName).Append("](https://beatsaver.com/maps/")
                .Append(song.Key).Append(")**")
                .Append(" (").Append(song.Key is { } key ? key.ToBsrKey() : "no !bsr").Append(")\n")
                .Append("Mapper(s): ").AppendLine(song.Info.MapperName)
                .Append("Difficulty: ").Append(version.Difficulty.Difficulty.ToString())
                .Append(", ").AppendLine(version.Difficulty.GameMode)
                .AppendLine();

            sb.Append("⭐: ").Append(rankedMap.Rating.DiffStar.ToString("0.00")).Append(" | ");
            sb.Append("✨: ").Append(rankedMap.Rating.AccStar.ToString("0.00"));

            if (rankedMap.Requirements.MinAccuracy is { } minAcc)
                sb.Append(" (Acc > ").Append(minAcc.ToString("0.##")).Append("%)");

            sb.AppendLine();

            if (rankedMap.CategoryIds.Length != 0)
            {
                sb.Append("Categories: ");
                var categoryNames = rankedMap.CategoryIds
                    .Select(id => categories.TryFirst(c => c.Id == id)
                        .Match(x => x.Info.Name, () => "Unknown"))
                    .ToArray();
                sb.Append(string.Join(", ", categoryNames));
            }

            sb.AppendLine()
                .Append("NJS: ").Append(version.Difficulty.Stats.NJS.ToString("0.##")).Append(" | ")
                .Append("Length: ")
                .Append(TimeSpan.FromSeconds(version.Song.Stats.DurationSec) switch
                {
                    { Hours: > 0 } ts => ts.ToString(@"hh\:mm\:ss"),
                    var ts => ts.ToString(@"mm\:ss")
                }).Append(" | ")
                .Append("BPM: ").Append(version.Song.Stats.BPM.ToString("0.##"))
                .AppendLine();

            if (rankedMap.Requirements.ProhibitedModifiers != RankedMapRequests.EModifiers.ProhibitedDefaults)
                sb.AppendLine()
                    .Append("Prohibited Modifiers: ")
                    .Append(rankedMap.Requirements.ProhibitedModifiers);

            if (rankedMap.Requirements.MandatoryModifiers != RankedMapRequests.EModifiers.None)
                sb.AppendLine()
                    .Append("Mandatory Modifiers: ")
                    .Append(rankedMap.Requirements.MandatoryModifiers | RankedMapRequests.EModifiers.FasterSong);

            if (rankedMap.Requirements.NeedConfirmation
                || rankedMap.Requirements.MaxPauseDurationSec is not null
                || rankedMap.Requirements.NeedFullCombo)
            {
                sb.AppendLine()
                    .Append("Requirements: ");

                if (rankedMap.Requirements.NeedFullCombo)
                    sb.Append("***FC***, ");

                if (rankedMap.Requirements.MaxPauseDurationSec is { } pauseSecs)
                    sb.Append("⏸️ < ").Append(pauseSecs.ToString("0.##")).Append("s, ");

                if (rankedMap.Requirements.NeedConfirmation)
                    sb.Append(emojiSettings.Value.NeedConfirmation).Append(", ");

                // Remove last ", "
                sb.Length -= 2;
            }

            var mapString = sb.ToString();

            var score = rankedScore.Score;

            sb.Clear()
                .Append(rankedScore switch
                {
                    RankedScoreResponses.RankedScore.InvalidRankedScore => ":x: ",
                    RankedScoreResponses.RankedScore.PendingRankedScore => ":hourglass: ",
                    RankedScoreResponses.RankedScore.AcceptedRankedScore => emojiSettings.Value.Confirmed,
                    RankedScoreResponses.RankedScore.RefusedRankedScore => emojiSettings.Value.Refused,
                    RankedScoreResponses.RankedScore.ValidRankedScore => ":white_check_mark: ",
                    _ => string.Empty
                }).Append(((float)Accuracy.From(
                        BaseScore.CreateUnsafe(score.BaseScore).Value,
                        MaxScore.CreateUnsafe(version.Difficulty.Stats.MaxScore).Value))
                    .ToString("N1")).Append("% ");

            if (rankedScore.PrevScore is { } prevScore)
            {
                var diff = score.BaseScore - prevScore.BaseScore;
                sb.Append(" (").Append(diff >= 0 ? "+" : "").Append(diff.ToString("N0")).Append(") ");
            }

            if (rankedScore.Score.Modifiers != RankedMapRequests.EModifiers.None)
                sb.Append(" | Mods: ").Append(rankedScore.Score.Modifiers).Append(' ');

            sb.Append(TimestampTag.FormatFromDateTimeOffset(score.SetAt, TimestampTagStyles.ShortDateTime))
                .AppendLine(score is RankedScoreResponses.Score.BeatLeaderScore
                {
                    BeatLeaderScoreId: { } blScoreId
                }
                    ? $" [Replay](https://replay.beatleader.com/?scoreId={blScoreId})"
                    : null);

            mapContainerBuilder.WithSection(x => x.WithTextDisplay(sb.ToString()).WithAccessory(new ThumbnailBuilder()
                .WithMedia("https://cdn.assets.beatleader.com/76561198134068431R18.png")));

            mapContainerBuilder.WithActionRow(y => y
                .WithButton(
                    label: "Accept",
                    customId: $"scoring-team-menu:score:{rankedScore.Score.Id}:accept",
                    style: ButtonStyle.Success)
                .WithButton(
                    label: "Refuse",
                    customId: $"scoring-team-menu:score:{rankedScore.Score.Id}:refuse",
                    style: ButtonStyle.Danger));

            mapContainerBuilder.WithSeparator();

            if (i > 0)
                mapContainerBuilder.WithTextDisplay(mapString);
            else
                mapContainerBuilder.WithSection(sectionBuilder
                    .WithTextDisplay(mapString)
                    .WithAccessory(new ThumbnailBuilder()
                        .WithMedia($"https://cdn.beatsaver.com/{rankedMap.Versions[0].Song.Hash}.jpg")));
        }

        return mapContainerBuilder;
    }
}