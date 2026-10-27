using System.Linq.Expressions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Shared;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ThreadResponse = GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses.Thread;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;

namespace GuildSaber.Api.Features.MapRequests.Services;

public sealed class MapRequestQueries(
    ServerDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<MapRequestSettings> settings,
    IOptions<MapRequestBoostSettings> boostSettings,
    IOptions<MapProposalReviewSettings> reviewSettings)
{
    public static Expression<Func<MapProposal, bool>> VisibleTo(PlayerId? viewer) => proposal
        => proposal is SubmittedProposal
           || proposal is AcceptedProposal
           || proposal is ClosedProposal &&
           ((ClosedProposal)proposal).SubmittedAt != null
           || viewer != null && proposal.AuthorId == viewer;

    public Task<MapRequestResponses.Proposal?> GetProposalAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, PlayerId? viewer,
        CancellationToken token)
        => dbContext.MapProposals.AsExpandable().AsSplitQuery()
            .Where(x => x.Id == proposalId && x.ThreadId == threadId && x.Thread.ContextId == contextId)
            .Where(VisibleTo(viewer))
            .Select(x => x.Map())
            .SingleOrDefaultAsync(token);

    public Task<ThreadResponse?> GetThreadAsync(
        ContextId contextId, MapRequestThreadId threadId, PlayerId? viewer, CancellationToken token)
        => dbContext.MapRequestThreads.AsExpandable().AsSplitQuery()
            .Where(x => x.Id == threadId && x.ContextId == contextId)
            .Where(x => x.Proposals.AsQueryable().Any(VisibleTo(viewer)) || viewer != null && x.AuthorId == viewer)
            .Select(MapRequestMappers.MapThreadExpression(viewer))
            .SingleOrDefaultAsync(token);

    public Task<PagedList<ThreadResponse>> GetQueueAsync(
        ContextId contextId,
        PlayerId? viewer,
        Filters filters,
        EMapRequestSorter sortBy,
        EOrder order,
        int page,
        int pageSize,
        CancellationToken token)
    {
        var now = timeProvider.GetUtcNow();
        var bonus = settings.Value.PriorityBonusPerBoost;
        var proposals = dbContext.MapProposals.OfType<SubmittedProposal>().ApplyFilters(filters);
        var query = dbContext.MapRequestThreads.AsExpandable().AsSplitQuery()
            .Where(x => x.ContextId == contextId && proposals.Any(proposal => proposal.ThreadId == x.Id))
            .Select(x => new
            {
                Thread = x,
                BoostCount = x.Boosts.Count,
                WaitingSince = x.Proposals.OfType<SubmittedProposal>().Min(proposal => proposal.SubmittedAt),
                CommunityScore = x.Proposals.OfType<SubmittedProposal>()
                    .SelectMany(proposal => proposal.Votes.OfType<CommunityProposalVote>())
                    .Sum(vote => vote.IsPositive ? 1 : -1)
            });

        return (sortBy switch
            {
                EMapRequestSorter.WaitingSince => query.OrderBy(order, x => x.WaitingSince),
                EMapRequestSorter.BoostCount => query.OrderBy(order, x => x.BoostCount).ThenBy(x => x.WaitingSince),
                EMapRequestSorter.CommunityVotes => query.OrderBy(order, x => x.CommunityScore)
                    .ThenBy(x => x.WaitingSince),
                _ => query.OrderBy(order, x => (now - x.WaitingSince).TotalSeconds * (1 + x.BoostCount * bonus))
            })
            .ThenBy(x => x.Thread.Id)
            .Select(x => MapRequestMappers.MapThreadExpression(viewer, true).Invoke(x.Thread))
            .ToPagedListAsync(page, pageSize, token);
    }

    public Task<PagedList<MapRequestResponses.Proposal>> GetMineAsync(
        ContextId contextId,
        PlayerId playerId,
        Filters filters,
        EMapProposalSorter sortBy,
        EOrder order,
        int page,
        int pageSize,
        CancellationToken token)
        => dbContext.MapProposals.AsExpandable().AsSplitQuery()
            .Where(x => x.Thread.ContextId == contextId && x.AuthorId == playerId)
            .ApplyFilters(filters)
            .ApplySortOrder(sortBy, order)
            .Select(x => x.Map())
            .ToPagedListAsync(page, pageSize, token);

    public Task<MapRequestResponses.BoostAllowance?> GetBoostsAsync(PlayerId playerId, CancellationToken token)
        => dbContext.Players.Where(x => x.Id == playerId)
            .Select(x => new MapRequestResponses.BoostAllowance(
                boostSettings.Value.GetLimit(x.SubscriptionInfo.Tier),
                x.RequestThreadBoosts
                    .OrderBy(x => x.ThreadId)
                    .Select(x => new MapRequestResponses.BoostAllocation(x.ThreadId, x.Thread.ContextId))
                    .ToArray()))
            .SingleOrDefaultAsync(token);

    public async Task<MapRequestResponses.Allowance> GetAllowanceAsync(
        ContextId contextId, PlayerId playerId, CancellationToken token)
    {
        var counts = await dbContext.MapProposals
            .Where(x => x.Thread.ContextId == contextId && x.AuthorId == playerId)
            .GroupBy(x => x.AuthorId)
            .Select(x => new
            {
                Drafts = x.Count(proposal => proposal is DraftProposal),
                RankingRequests =
                    x.Count(proposal => proposal is SubmittedProposal && proposal.Thread.RankedMap == null),
                RevisionRequests = x.Count(proposal
                    => proposal is SubmittedProposal && proposal.Thread.RankedMap != null)
            })
            .SingleOrDefaultAsync(token);

        return new MapRequestResponses.Allowance(
            counts?.Drafts ?? 0,
            settings.Value.MaxDraftsPerPlayer,
            counts?.RankingRequests ?? 0,
            settings.Value.MaxRankingRequestsPerPlayer,
            counts?.RevisionRequests ?? 0,
            settings.Value.MaxRevisionRequestsPerPlayer,
            settings.Value.MinimumAmendmentTier,
            reviewSettings.Value.MinimumPositiveVotes,
            reviewSettings.Value.RequireNoNegativeVotes,
            settings.Value.PriorityBonusPerBoost);
    }
}

public static class MapRequestQueryExtensions
{
    extension(IQueryable<MapProposal> query)
    {
        public IQueryable<MapProposal> ApplyFilters(Filters filters)
        {
            if (filters.IsRevision is { } isRevision)
                query = query.Where(x => x.Thread.RankedMap != null == isRevision);

            return query.ApplyMapSearch(filters.Search);
        }

        private IQueryable<MapProposal> ApplyMapSearch(string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            switch (search.Length)
            {
                case SongHash.ExactLength:
                    search = search.ToLowerInvariant();
                    return query.Where(x => x.MapVersions.Any(version => version.SongDifficulty.Song.Hash == search));
                case >= BeatSaverKey.BsrPrefixLength and <= BeatSaverKey.BsrPrefixLength + BeatSaverKey.MaxLength
                    when search.StartsWith("!bsr "):
                    search = search[5..];
                    return query.Where(x => x.MapVersions
                        .Any(version => version.SongDifficulty.Song.BeatSaverKey == search));
                default:
                    return query.Where(x => x.MapVersions.Any(version =>
                        EF.Functions.ILike(version.SongDifficulty.Song.Info.SongName, $"%{search}%") ||
                        EF.Functions.ILike(version.SongDifficulty.Song.Info.SongAuthorName, $"%{search}%") ||
                        EF.Functions.ILike(version.SongDifficulty.Song.Info.MapperName, $"%{search}%") ||
                        search.Length <= BeatSaverKey.MaxLength
                        && version.SongDifficulty.Song.BeatSaverKey != null
                        && EF.Functions.ILike(version.SongDifficulty.Song.BeatSaverKey, $"%{search}%")));
            }
        }

        public IOrderedQueryable<MapProposal> ApplySortOrder(EMapProposalSorter sortBy, EOrder order)
            => sortBy switch
            {
                EMapProposalSorter.Id => query.OrderBy(order, x => x.Id),
                _ => query.OrderBy(order, x => x.CreatedAt).ThenBy(order, x => x.Id)
            };
    }
}