using System.Linq.Expressions;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using Configuration = GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses.Configuration;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.Api.Features.MapRequests.Http;

public static class MapRequestMappers
{
    private static Func<MapProposal, Proposal>? _mapProposalImpl;
    private static Func<MapProposal, Configuration>? _mapConfigurationImpl;
    private static Func<MapProposal, Vote[]>? _mapVotesImpl;
    private static Func<MapProposalVote, Vote>? _mapVoteImpl;

    [Expandable(nameof(MapProposalExpression))]
    public static Proposal Map(this MapProposal self)
        => (_mapProposalImpl ??= MapProposalExpression().Compile())(self);

    [Expandable(nameof(MapConfigurationExpression))]
    public static Configuration MapConfiguration(this MapProposal self)
        => (_mapConfigurationImpl ??= MapConfigurationExpression().Compile())(self);

    [Expandable(nameof(MapVotesExpression))]
    public static Vote[] MapVotes(this MapProposal self)
        => (_mapVotesImpl ??= MapVotesExpression().Compile())(self);

    [Expandable(nameof(MapVoteExpression))]
    public static Vote Map(this MapProposalVote self)
        => (_mapVoteImpl ??= MapVoteExpression().Compile())(self);

    public static Expression<Func<MapProposal, Proposal>> MapProposalExpression()
        => self => CreateProposal(
            self.Id,
            self.ThreadId,
            self.AuthorId,
            self.CreatedAt,
            self.MapConfiguration(),
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
        MapProposalId id, MapRequestThreadId threadId, PlayerId? authorId, DateTimeOffset createdAt,
        Configuration configuration, Vote[] votes, DateTimeOffset? submittedAt, DateTimeOffset? acceptedAt,
        PlayerId? acceptedById, DateTimeOffset? closedAt, PlayerId? closedById, string? reason)
        => acceptedAt is { } accepted
            ? new Proposal.Accepted(
                id, threadId, authorId, createdAt, configuration, votes, submittedAt!.Value, accepted, acceptedById)
            : closedAt is { } closed
                ? new Proposal.Closed(
                    id, threadId, authorId, createdAt, configuration, votes, submittedAt, closed, closedById, reason)
                : submittedAt is { } submitted
                    ? new Proposal.Submitted(id, threadId, authorId, createdAt, configuration, votes, submitted)
                    : new Proposal.Draft(id, threadId, authorId, createdAt, configuration, votes);

    public static Expression<Func<MapProposal, Configuration>> MapConfigurationExpression()
        => self => new Configuration(
            self.Requirements.Map(),
            new Rating(
                self.Rating.AccStar == null ? null : self.Rating.AccStar.Value.Value,
                self.Rating.DiffStar == null ? null : self.Rating.DiffStar.Value.Value,
                self.Rating.IsAccStarAutoCalculated,
                self.Rating.IsDiffStarAutoCalculated),
            self.MapVersions.OrderBy(x => x.Order).ThenBy(x => x.SongDifficultyId).ThenBy(x => x.PlayModeId)
                .Select(x => new ProposalVersion(
                    x.SongDifficulty.Song.Hash,
                    x.SongDifficulty.Song.BeatSaverKey,
                    x.SongDifficulty.GameMode.Name,
                    x.SongDifficulty.Difficulty,
                    x.PlayModeId.Value,
                    x.PlayModeName,
                    x.Order))
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

    public static Expression<Func<MapRequestThread, MapRequestResponses.Thread>> MapThreadExpression(
        PlayerId? viewer, bool queuedOnly = false)
        => self => new MapRequestResponses.Thread(
            self.Id,
            self.ContextId,
            self.AuthorId,
            self.CreatedAt,
            self.RankedMap!.Id,
            self.Boosts.Count,
            viewer != null && self.Boosts.Any(x => x.PlayerId == viewer),
            self.IsLegacyImport,
            self.Proposals.AsQueryable()
                .Where(x => queuedOnly ? x is SubmittedProposal : MapRequestQueries.VisibleTo(viewer).Invoke(x))
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                .Select(x => x.Map())
                .ToArray());
}