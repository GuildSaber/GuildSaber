using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Api.Features.Scores.Http;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class RankedMapExtensions
{
    extension(RankedMapResponses.RankedMap self)
    {
        public ContainerBuilder ToContainerBuilder(
            RankedScoreResponses.RankedScore[] rankedScores,
            CategoryResponses.Category[] categories,
            IOptions<EmojiSettings> emojiSettings)
        {
            var sectionBuilder = new SectionBuilder();
            var mapContainerBuilder = new ContainerBuilder();

            foreach (var (i, version) in self.Versions.Index())
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

                sb.Append("⭐: ").Append(self.Rating.DiffStar.ToString("0.00")).Append(" | ");
                sb.Append("✨: ").Append(self.Rating.AccStar.ToString("0.00"));

                if (self.Requirements.MinAccuracy is { } minAcc)
                    sb.Append(" (Acc > ").Append(minAcc.ToString("0.##")).Append("%)");

                sb.AppendLine();

                if (self.CategoryIds.Length != 0)
                {
                    sb.Append("Categories: ");
                    var categoryNames = self.CategoryIds
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

                if (self.Requirements.ProhibitedModifiers != RankedMapRequests.EModifiers.ProhibitedDefaults)
                    sb.AppendLine()
                        .Append("Prohibited Modifiers: ")
                        .Append(self.Requirements.ProhibitedModifiers);

                if (self.Requirements.MandatoryModifiers != RankedMapRequests.EModifiers.None)
                    sb.AppendLine()
                        .Append("Mandatory Modifiers: ")
                        .Append(self.Requirements.MandatoryModifiers | RankedMapRequests.EModifiers.FasterSong);

                if (self.Requirements.NeedConfirmation
                    || self.Requirements.MaxPauseDurationSec is not null
                    || self.Requirements.NeedFullCombo)
                {
                    sb.AppendLine()
                        .Append("Requirements: ");

                    if (self.Requirements.NeedFullCombo)
                        sb.Append("***FC***, ");

                    if (self.Requirements.MaxPauseDurationSec is { } pauseSecs)
                        sb.Append("⏸️ < ").Append(pauseSecs.ToString("0.##")).Append("s, ");

                    if (self.Requirements.NeedConfirmation)
                        sb.Append(emojiSettings.Value.NeedConfirmation).Append(", ");

                    // Remove last ", "
                    sb.Length -= 2;
                }

                if (i > 0)
                    mapContainerBuilder.WithTextDisplay(sb.ToString());
                else
                    mapContainerBuilder.WithSection(sectionBuilder
                        .WithTextDisplay(sb.ToString())
                        .WithAccessory(new ThumbnailBuilder()
                            .WithMedia($"https://cdn.beatsaver.com/{self.Versions[0].Song.Hash}.jpg")));

                foreach (var rankedScore in rankedScores)
                {
                    mapContainerBuilder.WithSeparator();

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
                        .AppendLine(score is ScoreResponses.Score.BeatLeaderScore
                        {
                            BeatLeaderScoreId: { } blScoreId
                        }
                            ? $" [Replay](https://replay.beatleader.com/?scoreId={blScoreId})"
                            : null);

                    mapContainerBuilder.WithTextDisplay(sb.ToString());
                }
            }

            return mapContainerBuilder;
        }
    }
}