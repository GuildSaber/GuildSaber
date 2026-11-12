using System.Text;
using Discord;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.DiscordBot.Commands.Users;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class RankingThreadExtensions
{
    extension(MapRequestResponses.ThreadPreview self)
    {
        public ContainerBuilder ToContainerBuilder(int listPage = 1,
                                                   EMapRequestSorter sortBy = EMapRequestSorter.Priority,
                                                   UserModuleSlash.ERankingThreadOrigin origin =
                                                       UserModuleSlash.ERankingThreadOrigin.Queue)
        {
            var version = self.MapVersions[0];
            var proposals = self.Proposals
                .OrderByDescending(x => x.Id == self.PreviewProposalId)
                .ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id.Value)
                .Take(3).ToArray();

            var sb = new StringBuilder()
                .Append(version.Song.Key is { } key
                    ? $"**[{version.Song.Info.BeatSaverName}](https://beatsaver.com/maps/{key})** ({key.ToBsrKey()})"
                    : $"**{version.Song.Info.BeatSaverName}** (no !bsr)")
                .AppendLine()
                .Append("Mapper(s): ").AppendLine(version.Song.Info.MapperName)
                .Append("Difficulty: ").Append(version.Difficulty.Difficulty.ToString())
                .Append(", ").AppendLine(version.Difficulty.GameMode)
                .AppendLine();

            if (self.RankedMap is { } rankedMap)
                AppendRating(sb, rankedMap.Rating.DiffStar, rankedMap.Rating.AccStar, rankedMap.MinAccuracy);

            foreach (var proposal in proposals)
                AppendRating(sb.Append("-> "), proposal.Rating.DiffStar!.Value, proposal.Rating.AccStar!.Value,
                    proposal.MinAccuracy);

            if (self.RankedMap is not null || self.Proposals.Any(x => x.Categories.Length != 0))
            {
                sb.AppendLine();

                if (self.RankedMap is { } current)
                    sb.Append("Categories: ")
                        .Append(current.Categories.Length == 0 ? "None" : string.Join(", ", current.Categories.Take(4)))
                        .Append(current.Categories.Length > 4 ? $" (+{current.Categories.Length - 4} more)" : "")
                        .AppendLine();

                foreach (var proposal in proposals)
                    sb.Append("-> Categories: ")
                        .Append(proposal.Categories.Length == 0
                            ? "None"
                            : string.Join(", ", proposal.Categories.Take(4).Select(x => x.Name)))
                        .Append(proposal.Categories.Length > 4 ? $" (+{proposal.Categories.Length - 4} more)" : "")
                        .AppendLine();
            }

            sb.AppendLine()
                .Append("NJS: ").Append(version.Difficulty.Stats.NJS.ToString("0.##")).Append(" | ")
                .Append("Length: ")
                .Append(TimeSpan.FromSeconds(version.Difficulty.Stats.Duration) switch
                {
                    { Hours: > 0 } ts => ts.ToString(@"hh\:mm\:ss"),
                    var ts => ts.ToString(@"mm\:ss")
                }).Append(" | ")
                .Append("BPM: ").Append(version.Song.Stats.BPM.ToString("0.##"));

            if (self.Proposals.Length > 3)
                sb.AppendLine()
                    .Append($"And **{self.Proposals.Length - 3}** more alternatives. Open the thread to compare them.");

            return new ContainerBuilder()
                .WithAccentColor(Color.FromDifficulty(version.Difficulty.Difficulty))
                .WithSection(section => section.WithTextDisplay(sb.ToString())
                    .WithAccessory(new ThumbnailBuilder()
                        .WithMedia($"https://cfcdn.beatsaver.com/{version.Song.Hash}.jpg")))
                .WithSection(section => section
                    .WithTextDisplay(
                        $"**{self.Proposals.Length}** open {(self.Proposals.Length == 1 ? "proposal" : "proposals")}" +
                        (self.MapVersions.Length > 1 ? $" · **{self.MapVersions.Length}** map versions" : "") +
                        $"\n**RT:** +{self.RankingTeamVotes.Positive} / -{self.RankingTeamVotes.Negative}" +
                        $" · **Community:** +{self.CommunityVotes.Positive} / -{self.CommunityVotes.Negative}")
                    .WithAccessory(new ButtonBuilder()
                        .WithLabel(self.RankedMapId is null ? "Open New Thread" : "Open Edit Thread")
                        .WithCustomId(
                            $"ranking_thread:{self.ContextId},{self.Id},0,1,1,{listPage},{(int)sortBy},{(int)origin}")
                        .WithStyle(ButtonStyle.Secondary)))
                .WithRequester(self.PreviewAuthor);
        }
    }

    private static StringBuilder AppendRating(StringBuilder sb, float diffStar, float accStar, double? minAccuracy)
        => sb.Append("⭐: ").Append(diffStar.ToString("0.00")).Append(" | ✨: ").Append(accStar.ToString("0.00"))
            .Append(minAccuracy is { } accuracy ? $" (Acc ≥ {accuracy:0.##}%)" : "")
            .AppendLine();
}