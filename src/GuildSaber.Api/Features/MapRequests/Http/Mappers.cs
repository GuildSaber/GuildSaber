using System.Linq.Expressions;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using GuildSaber.Database.Models.Server.RankedMaps;
using Definition = GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses.Definition;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.Api.Features.MapRequests.Http;

public static class MapRequestMappers
{
    private static Func<MapProposal, Proposal>? _mapProposalImpl;
    private static Func<MapProposal, Definition>? _mapDefinitionImpl;
    private static Func<MapProposal, Vote[]>? _mapVotesImpl;
    private static Func<MapProposalVote, Vote>? _mapVoteImpl;
    private static Func<MapProposalVersion, MapPreview>? _mapPreviewImpl;
    private static Func<SubmittedProposal, ProposalMetadata>? _mapProposalMetadataImpl;
    private static Func<MapRequestThread, PlayerId?, MapRequestResponses.Thread>? _mapThreadImpl;
    private static Func<MapRequestThread, PlayerId?, ThreadPreview>? _mapThreadPreviewImpl;
    private static Func<RankedMap?, RankedMapMetadata?>? _mapRankedMapMetadataImpl;

    [Expandable(nameof(MapProposalExpression))]
    public static Proposal Map(this MapProposal self)
        => (_mapProposalImpl ??= MapProposalExpression().Compile())(self);

    [Expandable(nameof(MapDefinitionExpression))]
    public static Definition MapDefinition(this MapProposal self)
        => (_mapDefinitionImpl ??= MapDefinitionExpression().Compile())(self);

    [Expandable(nameof(MapVotesExpression))]
    public static Vote[] MapVotes(this MapProposal self)
        => (_mapVotesImpl ??= MapVotesExpression().Compile())(self);

    [Expandable(nameof(MapVoteExpression))]
    public static Vote Map(this MapProposalVote self)
        => (_mapVoteImpl ??= MapVoteExpression().Compile())(self);

    [Expandable(nameof(MapPreviewExpression))]
    public static MapPreview Map(this MapProposalVersion self)
        => (_mapPreviewImpl ??= MapPreviewExpression().Compile())(self);

    [Expandable(nameof(MapProposalMetadataExpression))]
    public static ProposalMetadata MapMetadata(this SubmittedProposal self)
        => (_mapProposalMetadataImpl ??= MapProposalMetadataExpression().Compile())(self);

    [Expandable(nameof(MapThreadExpression))]
    public static MapRequestResponses.Thread Map(this MapRequestThread self, PlayerId? viewer)
        => (_mapThreadImpl ??= MapThreadExpression().Compile())(self, viewer);

    [Expandable(nameof(MapThreadPreviewExpression))]
    public static ThreadPreview MapPreview(this MapRequestThread self, PlayerId? viewer)
        => (_mapThreadPreviewImpl ??= MapThreadPreviewExpression().Compile())(self, viewer);

    [Expandable(nameof(MapRankedMapMetadataExpression))]
    public static RankedMapMetadata? MapMetadata(this RankedMap? self)
        => (_mapRankedMapMetadataImpl ??= MapRankedMapMetadataExpression().Compile())(self);

    public static Expression<Func<RankedMap?, RankedMapMetadata?>> MapRankedMapMetadataExpression()
        => self => self == null
            ? null
            : new RankedMapMetadata(
                self.Rating.Map(),
                self.Requirements.MinAccuracy,
                self.Categories.OrderBy(x => x.Id).Select(x => (string)x.Info.Name).ToArray());

    public static Expression<Func<MapProposal, Proposal>> MapProposalExpression()
        => self => CreateProposal(
            self.Id,
            self.ThreadId,
            self.AuthorId,
            self.Author == null ? null : self.Author.Map(),
            self.CreatedAt,
            self.MapDefinition(),
            self.MapVotes(),
            self is SubmittedProposal
                ? ((SubmittedProposal)self).SubmittedAt
                : self is AcceptedProposal
                    ? ((AcceptedProposal)self).SubmittedAt
                    : self is ClosedProposal
                        ? ((ClosedProposal)self).SubmittedAt
                        : null,
            self is AcceptedProposal ? ((AcceptedProposal)self).AcceptedAt : null,
            self is AcceptedProposal ? ((AcceptedProposal)self).AcceptedById : null,
            self is ClosedProposal ? ((ClosedProposal)self).ClosedAt : null,
            self is ClosedProposal ? ((ClosedProposal)self).ClosedById : null,
            self is ClosedProposal ? ((ClosedProposal)self).Reason : null);

    // Keep collection projections outside the lifecycle branches: otherwise EF repeats each split query per subtype.
    // Only response values reach this final projection. No entity graph is loaded for mapping.
    private static Proposal CreateProposal(
        MapProposalId id, MapRequestThreadId threadId, PlayerId? authorId, PlayerResponses.Player? author,
        DateTimeOffset createdAt, Definition definition, Vote[] votes, DateTimeOffset? submittedAt,
        DateTimeOffset? acceptedAt,
        PlayerId? acceptedById, DateTimeOffset? closedAt, PlayerId? closedById, string? reason)
        => acceptedAt is { } accepted
            ? new Proposal.Accepted(
                id, threadId, authorId, author, createdAt, definition, votes, submittedAt!.Value, accepted,
                acceptedById)
            : closedAt is { } closed
                ? new Proposal.Closed(
                    id, threadId, authorId, author, createdAt, definition, votes, submittedAt, closed, closedById,
                    reason)
                : submittedAt is { } submitted
                    ? new Proposal.Submitted(id, threadId, authorId, author, createdAt, definition, votes, submitted)
                    : new Proposal.Draft(id, threadId, authorId, author, createdAt, definition, votes);

    public static Expression<Func<MapProposalVersion, MapPreview>> MapPreviewExpression()
        => self => new MapPreview(
            self.SongDifficulty.Song.Map(),
            self.SongDifficulty.Map(),
            self.PlayModeId.Value,
            self.PlayModeName,
            self.Order);

    public static Expression<Func<SubmittedProposal, ProposalMetadata>> MapProposalMetadataExpression()
        => self => new ProposalMetadata(
            self.Id,
            self.Author == null ? null : self.Author.Map(),
            self.CreatedAt,
            self.SubmittedAt,
            new Rating(
                self.Rating.AccStar == null ? null : self.Rating.AccStar.Value.Value,
                self.Rating.DiffStar == null ? null : self.Rating.DiffStar.Value.Value,
                self.Rating.IsAccStarAutoCalculated,
                self.Rating.IsDiffStarAutoCalculated),
            self.Requirements.MinAccuracy,
            self.Categories.OrderBy(x => x.CategoryId)
                .Select(x => new CategorySnapshot(x.CategoryId, x.Name))
                .ToArray());

    public static Expression<Func<MapProposal, Definition>> MapDefinitionExpression()
        => self => new Definition(
            self.Requirements.Map(),
            new Rating(
                self.Rating.AccStar == null ? null : self.Rating.AccStar.Value.Value,
                self.Rating.DiffStar == null ? null : self.Rating.DiffStar.Value.Value,
                self.Rating.IsAccStarAutoCalculated,
                self.Rating.IsDiffStarAutoCalculated),
            self.MapVersions.OrderBy(x => x.Order).ThenBy(x => x.SongDifficultyId).ThenBy(x => x.PlayModeId)
                .Select(x => x.Map())
                .ToArray(),
            self.Categories.OrderBy(x => x.CategoryId)
                .Select(x => new CategorySnapshot(x.CategoryId, x.Name))
                .ToArray(),
            self.Achievements.OrderBy(x => x.AchievementId)
                .Select(x => new AchievementSnapshot(x.AchievementId.Value, x.Name))
                .ToArray(),
            self.Comment);

    public static Expression<Func<MapProposal, Vote[]>> MapVotesExpression()
        => self => self.Votes.OrderBy(x => x.VotedAt).ThenBy(x => x.PlayerId)
            .Select(x => x.Map())
            .ToArray();

    public static Expression<Func<MapProposalVote, Vote>> MapVoteExpression()
        => self => self is CommunityProposalVote
            ? new Vote.Community(self.PlayerId, self.IsPositive, self.VotedAt)
            : new Vote.RankingTeam(self.PlayerId, self.IsPositive, self.VotedAt,
                ((RankingTeamProposalVote)self).Reason);

    public static Expression<Func<MapRequestThread, PlayerId?, MapRequestResponses.Thread>> MapThreadExpression()
        => (self, viewer) => new MapRequestResponses.Thread(
            self.Id,
            self.ContextId,
            self.AuthorId,
            self.CreatedAt,
            self.RankedMap == null ? null : self.RankedMap.Id,
            self.Boosts.Count,
            viewer != null && self.Boosts.Any(x => x.PlayerId == viewer),
            self.IsLegacyImport,
            self.Proposals.AsQueryable()
                .Where(MapRequestQueries.VisibleTo(viewer))
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                .Select(x => x.Map())
                .ToArray());

    public static Expression<Func<MapRequestThread, PlayerId?, ThreadPreview>> MapThreadPreviewExpression()
        => (self, viewer) => self.Proposals.OfType<SubmittedProposal>().AsQueryable()
            .OrderByDescending(x => self.AuthorId != null && x.AuthorId == self.AuthorId)
            .ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(proposal => new ThreadPreview(
                self.Id,
                self.ContextId,
                self.AuthorId,
                self.CreatedAt,
                self.RankedMap == null ? null : self.RankedMap.Id,
                self.Boosts.Count,
                viewer != null && self.Boosts.Any(x => x.PlayerId == viewer),
                self.IsLegacyImport,
                proposal.Id,
                proposal.Author == null ? null : proposal.Author.Map(),
                proposal.MapVersions.OrderBy(x => x.Order).ThenBy(x => x.SongDifficultyId).ThenBy(x => x.PlayModeId)
                    .Select(x => x.Map())
                    .ToArray(),
                self.Proposals.OfType<SubmittedProposal>().AsQueryable()
                    .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                    .Select(x => x.MapMetadata())
                    .ToArray(),
                new VoteCounts(
                    self.Proposals.OfType<SubmittedProposal>().SelectMany(x => x.Votes)
                        .Count(x => x is CommunityProposalVote && x.IsPositive),
                    self.Proposals.OfType<SubmittedProposal>().SelectMany(x => x.Votes)
                        .Count(x => x is CommunityProposalVote && !x.IsPositive)),
                new VoteCounts(
                    self.Proposals.OfType<SubmittedProposal>().SelectMany(x => x.Votes)
                        .Count(x => x is RankingTeamProposalVote && x.IsPositive),
                    self.Proposals.OfType<SubmittedProposal>().SelectMany(x => x.Votes)
                        .Count(x => x is RankingTeamProposalVote && !x.IsPositive)),
                self.RankedMap.MapMetadata()))
            .First();
}