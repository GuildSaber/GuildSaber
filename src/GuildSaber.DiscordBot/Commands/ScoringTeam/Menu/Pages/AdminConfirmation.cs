using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.CSharpClient;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Commands.ScoringTeam;

public partial class ScoringTeamModuleSlash
{
    [ComponentInteraction("scoring_admin_conf:*,*")]
    public async Task AdminConfirmation(ContextId contextId, int page)
    {
        var guildId = await GetGuildIdAsync();
        var categories = await Cache.GetGuildCategoriesAsync(guildId, Client.Value);

        var filter = new RankedScoreRequests.Filters(RankedScoreTypes: RankedScoreRequests.ERankedScoreType.Pending);
        var sorter = new PaginatedRequestOptions<RankedScoreRequests.ERankedScoreSorter>
        {
            Page = page,
            PageSize = 3,
            SortBy = RankedScoreRequests.ERankedScoreSorter.ScoreTime,
            Order = EOrder.Asc
        };

        var pendingScores = await Client.Value.RankedScores
            .GetAsync(contextId, filter, sorter)
            .Unwrap();

        var component = (await ScoringTeamMenuView
                .BuildComponent(pendingScores.Data, categories, EmojiSettings, Client.Value))
            .WithActionRow(x => x
                .WithButton(label: "Back to menu", customId: $"scoring:{contextId}",
                    style: ButtonStyle.Secondary)
                .WithButton(
                    label: "Prev",
                    disabled: !pendingScores.HasPreviousPage,
                    customId: $"scoring_admin_conf:{contextId},{pendingScores.Page - 1}",
                    style: ButtonStyle.Primary)
                .WithButton(
                    label: "Next",
                    disabled: !pendingScores.HasNextPage,
                    customId: $"scoring_admin_conf:{contextId},{pendingScores.Page + 1}",
                    style: ButtonStyle.Primary))
            .Build();

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(x => x.Components = component);
    }
}

file static class ScoringTeamMenuView
{
    public static async Task<ComponentBuilderV2> BuildComponent(
        RankedScoreResponses.RankedScoreWithRankedMap[] pendingScores,
        CategoryResponses.Category[] categories,
        IOptions<EmojiSettings> emojiSettings,
        GuildSaberClient client)
    {
        var builder = new ComponentBuilderV2();

        if (pendingScores.Length == 0)
            return builder.WithTextDisplay("No pending scores found.");

        foreach (var data in pendingScores)
        {
            var (player, scoreStatistics) = await (
                client.Players.GetByIdAsync(data.RankedScore.PlayerId)
                    .EnsureNotNull("Player not found")
                    .Unwrap(),
                client.Scores.GetStatisticsAsync(data.RankedScore.Score.Id).Unwrap()
            ).WhenAll();

            var score = data.RankedScore.Score;
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(":hourglass:").Append(((float)Accuracy.From(
                    BaseScore.CreateUnsafe(score.BaseScore).Value,
                    MaxScore.CreateUnsafe(data.RankedMap.Versions.First(x => x.Difficulty.Id == score.SongDifficultyId)
                        .Difficulty.Stats.MaxScore).Value))
                .ToString("N1")).Append("% ");

            if (data.RankedScore.PrevScore is { } prevScore)
            {
                var diff = score.BaseScore - prevScore.BaseScore;
                stringBuilder.Append(" (").Append(diff >= 0 ? "+" : "").Append(diff.ToString("N0")).AppendLine(") ");
            }

            stringBuilder.AppendLine(
                TimestampTag.FormatFromDateTimeOffset(score.SetAt, TimestampTagStyles.ShortDateTime));

            var mods = data.RankedScore.Score.Modifiers;
            if (mods != RankedMapRequests.EModifiers.None)
                stringBuilder.Append("Mods: **").Append(mods.ToModifiersString()).AppendLine("**");

            if (score.IsFullCombo)
                stringBuilder.AppendLine("**FC**");
            else
                stringBuilder.Append("Misses: **").Append(score.MissedNotes).AppendLine("**");

            if (scoreStatistics is null)
            {
                stringBuilder.AppendLine("> No other stats available for this score.");
            }
            else
            {
                if (scoreStatistics.WinTracker.PauseCount > 0)
                    stringBuilder.Append($"Pause duration: **{scoreStatistics.WinTracker.TotalPauseDuration:N1}sec**" +
                                         $" in **{scoreStatistics.WinTracker.PauseCount}** pauses");
            }

            var blScoreId = (score as RankedScoreResponses.Score.BeatLeaderScore)?.BeatLeaderScoreId;
            builder.WithContainer(data.RankedMap.ToContainerBuilder([], categories, emojiSettings)
                    .WithSeparator()
                    .WithSection(x => x
                        .WithTextDisplay(
                            $"### [{player.PlayerInfo.Username}](https://beatleader.com/u/{player.PlayerLinkedAccounts.BeatLeaderId})\n{stringBuilder}")
                        .WithAccessory(new ThumbnailBuilder().WithMedia(player.PlayerInfo.AvatarUrl))))
                .WithActionRow(y => y
                    .WithButton(
                        label: "Accept",
                        customId: $"scoring-team-menu:score:{data.RankedScore.Score.Id}:accept",
                        style: ButtonStyle.Success)
                    .WithButton(
                        label: "Refuse",
                        customId: $"scoring-team-menu:score:{data.RankedScore.Score.Id}:refuse",
                        style: ButtonStyle.Danger)
                    .WithButton(
                        label: "View Replay",
                        url: $"https://replay.beatleader.com/?scoreId={blScoreId}",
                        disabled: blScoreId is null,
                        style: ButtonStyle.Link));
        }

        return builder;
    }
}