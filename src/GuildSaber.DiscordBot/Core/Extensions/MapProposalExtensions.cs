using System.Text;
using Discord;
using GuildSaber.Api.Features.RankedMaps.Http;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class MapProposalExtensions
{
    extension(Proposal self)
    {
        public string StateLabel => self switch
        {
            Proposal.Draft => "Private draft",
            Proposal.Submitted => "Open proposal",
            Proposal.Accepted => "Accepted proposal",
            Proposal.Closed => "Closed proposal",
            _ => throw new ArgumentOutOfRangeException(nameof(self))
        };

        public DateTimeOffset EventTime => self switch
        {
            Proposal.Submitted proposal => proposal.SubmittedAt,
            Proposal.Accepted proposal => proposal.AcceptedAt,
            Proposal.Closed proposal => proposal.ClosedAt,
            _ => self.CreatedAt
        };

        public ContainerBuilder ToContainerBuilder(Proposal.Accepted? current, int versionPage)
        {
            var definition = self.GetPreviewDefinition();
            var version = definition.MapVersions[versionPage - 1];
            var song = version.Song;
            var sb = new StringBuilder()
                .Append(self.Change is ProposalChange.Unrank ? "**Unranking request**\n" : "")
                .Append(song.Key is { } key
                    ? $"### [{song.Info.BeatSaverName}](https://beatsaver.com/maps/{key}) ({key.ToBsrKey()})"
                    : $"### {song.Info.BeatSaverName}")
                .AppendLine()
                .Append("Mapper(s): ").AppendLine(song.Info.MapperName)
                .Append("Difficulty: ").Append(version.Difficulty.Difficulty.ToString())
                .Append(", ").AppendLine(version.Difficulty.GameMode)
                .Append("Play mode: ").Append(version.PlayModeName)
                .AppendLine(definition.MapVersions.Length > 1
                    ? $" · Map version {versionPage}/{definition.MapVersions.Length}"
                    : "")
                .AppendLine()
                .Append("⭐: ").Append(definition.Rating.DiffStar?.ToString("0.00") ?? "Auto on submission")
                .Append(definition.Rating.IsDiffStarAutoCalculated ? " (auto)" : "")
                .Append(" | ✨: ").Append(definition.Rating.AccStar?.ToString("0.00") ?? "Auto on submission")
                .AppendLine(definition.Rating.IsAccStarAutoCalculated ? " (auto)" : "")
                .Append("Categories: ").AppendLine(definition.Categories.Length == 0
                    ? "None"
                    : string.Join(", ", definition.Categories.Select(x => x.Name)))
                .Append("NJS: ").Append(version.Difficulty.Stats.NJS.ToString("0.##"))
                .Append(" | Length: ").Append(TimeSpan.FromSeconds(version.Difficulty.Stats.Duration) switch
                {
                    { Hours: > 0 } ts => ts.ToString(@"hh\:mm\:ss"),
                    var ts => ts.ToString(@"mm\:ss")
                }).Append(" | BPM: ").AppendLine(song.Stats.BPM.ToString("0.##"))
                .Append("Score requirements: ").AppendLine(definition.Requirements.ToDisplayText());

            if (definition.Achievements.Length != 0)
                sb.Append("Achievements: ")
                    .AppendLine(string.Join(", ", definition.Achievements.Select(x => x.Name)));

            var builder = new ContainerBuilder()
                .WithAccentColor(self switch
                {
                    Proposal.Accepted => Color.Green,
                    Proposal.Closed => Color.DarkGrey,
                    _ => Color.FromDifficulty(version.Difficulty.Difficulty)
                })
                .WithSection(section => section.WithTextDisplay(sb.ToString())
                    .WithAccessory(new ThumbnailBuilder().WithMedia($"https://cfcdn.beatsaver.com/{song.Hash}.jpg")))
                .WithTextDisplay($"**{self.StateLabel} #{self.Id}** · " +
                                 TimestampTag.FormatFromDateTimeOffset(self.EventTime, TimestampTagStyles.Relative) +
                                 self switch
                                 {
                                     Proposal.Accepted when current?.Id == self.Id => " · Current",
                                     Proposal.Accepted => " · Historical",
                                     _ => ""
                                 })
                .WithRequester(self.Author, "Proposed by");

            if (self is Proposal.Submitted { Change: ProposalChange.Rank }
                && current is { Change: ProposalChange.Rank })
                builder.WithTextDisplay(
                    $"**Changes from accepted proposal #{current.Id}**\n{definition.ChangesFrom(current.GetPreviewDefinition())}");

            if (self.Change is ProposalChange.Unrank unrank)
                builder.WithTextDisplay(
                    $"Targets accepted proposal **#{unrank.TargetAcceptedProposalId}**");

            var comment = self.Change is ProposalChange.Unrank removal ? removal.Reason : self.Comment;
            if (!string.IsNullOrWhiteSpace(comment))
                builder.WithTextDisplay(
                    $"**{(self.Change is ProposalChange.Unrank ? "Unranking reason" : "Author's note")}**\n{comment}");

            if (self is Proposal.Closed { Reason: { Length: > 0 } reason })
                builder.WithTextDisplay($"**Closure reason**\n{reason[..Math.Min(reason.Length, 1000)]}");

            return builder;
        }
    }

    extension(RankedMapRequests.RankedMapRequirements self)
    {
        public string ToDisplayText() => string.Join(" · ", new[]
        {
            self.MinAccuracy is { } accuracy ? $"Accuracy ≥ {accuracy:0.##}%" : null,
            self.NeedFullCombo ? "Full combo" : null,
            self.NeedConfirmation ? "Score confirmation" : null,
            self.MaxPauseDurationSec is { } pause ? $"Total pause ≤ {pause:0.##}s" : null,
            self.MandatoryModifiers != RankedMapRequests.EModifiers.None
                ? $"Required modifiers: {self.MandatoryModifiers}"
                : null,
            self.ProhibitedModifiers != RankedMapRequests.EModifiers.ProhibitedDefaults
                ? $"Forbidden modifiers: {self.ProhibitedModifiers}"
                : null
        }.OfType<string>().DefaultIfEmpty("Default requirements"));
    }

    extension(Definition self)
    {
        public CreateDraft.Rank ToDraft(string? comment = null) => new(
            self.MapVersions.Select(x => new MapVersionInput(new SongReference.ByHash(x.Song.Hash),
                x.Difficulty.GameMode, x.Difficulty.Difficulty, x.PlayModeName, x.Order)).ToArray(),
            self.Requirements,
            new RankedMapRequests.ManualRating(
                self.Rating.IsDiffStarAutoCalculated ? null : self.Rating.DiffStar,
                self.Rating.IsAccStarAutoCalculated ? null : self.Rating.AccStar),
            self.Categories.Select(x => x.Id).ToArray(),
            self.Achievements.Select(x => x.Id).ToArray(),
            comment);

        public string ChangesFrom(Definition previous)
        {
            var sb = new StringBuilder();

            if (!Nullable.Equals(self.Rating.DiffStar, previous.Rating.DiffStar))
                sb.Append("⭐: ").Append(previous.Rating.DiffStar?.ToString("0.00"))
                    .Append(" -> ").AppendLine(self.Rating.DiffStar?.ToString("0.00"));
            if (!Nullable.Equals(self.Rating.AccStar, previous.Rating.AccStar))
                sb.Append("✨: ").Append(previous.Rating.AccStar?.ToString("0.00"))
                    .Append(" -> ").AppendLine(self.Rating.AccStar?.ToString("0.00"));
            if (!self.Categories.Select(x => x.Id).SequenceEqual(previous.Categories.Select(x => x.Id)))
                sb.Append("Categories: ")
                    .Append(previous.Categories.Length == 0
                        ? "None"
                        : string.Join(", ", previous.Categories.Select(x => x.Name)))
                    .Append(" -> ")
                    .AppendLine(self.Categories.Length == 0
                        ? "None"
                        : string.Join(", ", self.Categories.Select(x => x.Name)));
            if (self.Requirements != previous.Requirements)
                sb.Append("Score requirements: ").Append(previous.Requirements.ToDisplayText())
                    .Append(" -> ").AppendLine(self.Requirements.ToDisplayText());
            if (!self.MapVersions.Select(x => (x.Song.Hash, x.Difficulty.Id, x.PlayModeId, x.Order))
                    .SequenceEqual(previous.MapVersions.Select(x
                        => (x.Song.Hash, x.Difficulty.Id, x.PlayModeId, x.Order))))
                sb.AppendLine("Map versions or play modes changed. Use the map-version controls to inspect them.");
            if (!self.Achievements.Select(x => x.Id).SequenceEqual(previous.Achievements.Select(x => x.Id)))
                sb.AppendLine("Achievement associations changed.");

            return sb.Length == 0 ? "No ranking changes." : sb.ToString();
        }
    }
}