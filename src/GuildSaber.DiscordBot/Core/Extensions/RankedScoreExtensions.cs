using System.Diagnostics;
using System.Text;
using Discord;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Api.Features.Scores.Http;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Options;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreResponses.RankedScore;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class RankedScoreExtensions
{
    extension(RankedScoreResponses.RankedScore self)
    {
        public SectionBuilder ToSection(
            PlayerResponses.Player player,
            RankedMapResponses.RankedMap rankedMap,
            ScoreResponses.ScoreStatistics? scoreStatistic,
            IOptions<EmojiSettings> emojiSettings)
        {
            var builder = new SectionBuilder();
            var stringBuilder = new StringBuilder();

            var score = self.Score;
            stringBuilder.Append(self switch
            {
                InvalidRankedScore => ":x: ",
                PendingRankedScore => ":hourglass: ",
                AcceptedRankedScore => emojiSettings.Value.Confirmed,
                RefusedRankedScore => emojiSettings.Value.Refused,
                ValidRankedScore => ":white_check_mark: ",
                _ => throw new UnreachableException()
            }).Append(((float)Accuracy.From(
                    BaseScore.CreateUnsafe(score.BaseScore).Value,
                    MaxScore.CreateUnsafe(rankedMap.Versions
                        .First(x => x.Difficulty.Id == score.SongDifficultyId)
                        .Difficulty.Stats.MaxScore).Value))
                .ToString("N1")).Append("% ");

            if (self.PrevScore is { } prevScore)
            {
                var diff = score.BaseScore - prevScore.BaseScore;
                stringBuilder.Append(" (").Append(diff >= 0 ? "+" : "").Append(diff.ToString("N0"))
                    .AppendLine(") ");
            }

            stringBuilder.AppendLine(
                TimestampTag.FormatFromDateTimeOffset(score.SetAt, TimestampTagStyles.ShortDateTime));

            var mods = score.Modifiers;
            if (mods != RankedMapRequests.EModifiers.None)
                stringBuilder.Append("Mods: **").Append(mods.ToModifiersString()).AppendLine("**");

            if (score.IsFullCombo)
                stringBuilder.AppendLine("**FC**");
            else
                stringBuilder.Append("Misses: **").Append(score.MissedNotes).AppendLine("**");

            if (scoreStatistic is null)
            {
                stringBuilder.AppendLine("> No other stats available for this score.");
            }
            else
            {
                if (scoreStatistic.WinTracker.PauseCount > 0)
                    stringBuilder.Append(
                        $"Pause duration: **{scoreStatistic.WinTracker.TotalPauseDuration:N1}sec**" +
                        $" in **{scoreStatistic.WinTracker.PauseCount}** pauses");
            }

            builder.WithTextDisplay(
                    $"### [{player.PlayerInfo.Username}](https://beatleader.com/u/{player.PlayerLinkedAccounts.BeatLeaderId})\n{stringBuilder}")
                .WithAccessory(new ThumbnailBuilder().WithMedia(player.PlayerInfo.AvatarUrl));

            return builder;
        }
    }
}