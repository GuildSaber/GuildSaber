using System.Text.Json.Serialization;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
#if NET10_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
#endif

namespace GuildSaber.Api.Features.MapRequests.Http;

public static class MapRequestResponses
{
    /// <summary>
    /// Read-only BeatSaver metadata for choosing the first difficulty of a new request.
    /// </summary>
    /// <param name="Song">Selected song version. Use its hash to retain the version shown in the preview.</param>
    /// <param name="Difficulties">Difficulties with supported characteristics and a positive maximum score.</param>
    /// <param name="PlayModes">Supported GuildSaber play-mode names.</param>
    public sealed record SongLookup(SongSource Song, DifficultyOption[] Difficulties, string[] PlayModes);

    /// <summary>Song metadata before a song or difficulty has been imported.</summary>
    public sealed record SongSource(
        BeatSaverKey Key,
        SongHash Hash,
        RankedMapResponses.SongInfo Info,
        RankedMapResponses.SongStats Stats);

    /// <summary>A playable difficulty offered when choosing a new ranking request's map.</summary>
    public sealed record DifficultyOption(
        EDifficulty Difficulty,
        string Characteristic,
        RankedMapResponses.SongDifficultyStats Stats);

    /// <summary>
    /// A thread using the requested difficulty. Includes current rankings, submitted proposals, your private drafts
    /// and published history. Private draft matches are visible to their author.
    /// </summary>
    /// <param name="ThreadId">Thread to open when proposing changes to the existing request or ranked map.</param>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(Ranked), "Ranked")]
    [JsonDerivedType(typeof(Open), "Open")]
    [JsonDerivedType(typeof(Draft), "Draft")]
    [JsonDerivedType(typeof(History), "History")]
    public closed record ThreadMatch(MapRequestThreadId ThreadId)
    {
        /// <summary>A current ranked map containing the requested difficulty.</summary>
        public sealed record Ranked(MapRequestThreadId ThreadId, RankedMapId RankedMapId) : ThreadMatch(ThreadId);

        /// <summary>A submitted proposal containing the requested difficulty.</summary>
        public sealed record Open(MapRequestThreadId ThreadId, MapProposalId ProposalId) : ThreadMatch(ThreadId);

        /// <summary>Your private draft containing the requested difficulty.</summary>
        public sealed record Draft(MapRequestThreadId ThreadId, MapProposalId ProposalId) : ThreadMatch(ThreadId);

        /// <summary>A previously published proposal containing the requested difficulty.</summary>
        public sealed record History(MapRequestThreadId ThreadId, MapProposalId ProposalId) : ThreadMatch(ThreadId);
    }

    /// <summary>
    /// A queued thread with map previews, submitted proposal metadata and vote counts.
    /// Full definitions and individual votes are omitted.
    /// </summary>
    /// <param name="Id">Thread identity, used to open the full thread.</param>
    /// <param name="ContextId">Context to which the thread permanently belongs.</param>
    /// <param name="AuthorId">Original thread creator. Absent for imports or when the player was deleted.</param>
    /// <param name="CreatedAt">Thread creation time, not the waiting-time origin used to sort the queue.</param>
    /// <param name="RankedMapId">Current ranked map. Absent for a new ranking or when the ranked map was deleted.</param>
    /// <param name="BoostCount">Number of active thread boosts.</param>
    /// <param name="IsBoostedByMe">Whether the authenticated viewer boosts this thread. False for anonymous viewers.</param>
    /// <param name="IsLegacyImport">Whether the thread belongs to the legacy-import synchronization path.</param>
    /// <param name="PreviewProposalId">
    /// Latest submitted proposal by the thread creator, or the latest submitted proposal when none matches.
    /// Creation time and proposal ID determine which is latest. Private drafts and historical proposals are excluded.
    /// </param>
    /// <param name="PreviewAuthor">Author of the selected preview proposal. Absent for imports or a deleted player.</param>
    /// <param name="MapVersions">Map previews from the selected proposal, in ascending display order.</param>
    /// <param name="Proposals">Submitted proposal metadata in creation order. Its length is the submitted proposal count.</param>
    /// <param name="CommunityVotes">Community vote counts across all submitted proposals in the thread.</param>
    /// <param name="RankingTeamVotes">Ranking-team vote counts across all submitted proposals in the thread.</param>
    /// <param name="RankedMap">Current ranked-map values to compare proposals against. Absent for unranked threads.</param>
    public sealed record ThreadPreview(
        MapRequestThreadId Id,
        ContextId ContextId,
        PlayerId? AuthorId,
        DateTimeOffset CreatedAt,
        RankedMapId? RankedMapId,
        int BoostCount,
        bool IsBoostedByMe,
        bool IsLegacyImport,
        MapProposalId PreviewProposalId,
        PlayerResponses.Player? PreviewAuthor,
        MapPreview[] MapVersions,
        ProposalMetadata[] Proposals,
        VoteCounts CommunityVotes,
        VoteCounts RankingTeamVotes,
        RankedMapMetadata? RankedMap);

    /// <summary>
    /// Current ranked-map values shown before the proposed changes in the queue.
    /// These come from the live ranked map, not a historical accepted proposal.
    /// </summary>
    /// <param name="Rating">Current ranked-map ratings.</param>
    /// <param name="MinAccuracy">Current minimum qualifying score accuracy in percent. Null imposes no minimum.</param>
    /// <param name="Categories">Current category names, ordered by category ID.</param>
    public sealed record RankedMapMetadata(
        RankedMapResponses.RankedMapRating Rating,
        double? MinAccuracy,
        string[] Categories);

    /// <summary>
    /// Positive and negative votes on currently submitted proposals. Drafts and historical proposals are excluded.
    /// The same player voting on different proposals contributes one vote for each proposal.
    /// </summary>
    /// <param name="Positive">Number of positive votes.</param>
    /// <param name="Negative">Number of negative votes.</param>
    public readonly record struct VoteCounts(int Positive, int Negative);

    /// <summary>
    /// Resolved song, difficulty and play mode retained by a proposal or displayed in the queue.
    /// </summary>
    /// <param name="Song">Exact proposed song version and its stored information, not the latest BeatSaver version.</param>
    /// <param name="Difficulty">Resolved song difficulty, characteristic, leaderboards and statistics.</param>
    /// <param name="PlayModeId">Historical play-mode identity.</param>
    /// <param name="PlayModeName">Play-mode name captured when the proposal was created.</param>
    /// <param name="Order">Ascending display order within the proposal.</param>
    public sealed record MapPreview(
        RankedMapResponses.Song Song,
        RankedMapResponses.SongDifficulty Difficulty,
        int PlayModeId,
        string PlayModeName,
        byte Order);

    /// <summary>
    /// Submitted proposal information needed to compare ratings and categories in the queue.
    /// </summary>
    /// <param name="Id">Proposal identity, used to open its details.</param>
    /// <param name="Author">Proposal author. Absent for imports or when the player was deleted.</param>
    /// <param name="CreatedAt">Proposal creation time.</param>
    /// <param name="SubmittedAt">Time the proposal was published to the queue.</param>
    /// <param name="Rating">Resolved ratings and whether they were calculated automatically.</param>
    /// <param name="MinAccuracy">Minimum qualifying score accuracy in percent. Null imposes no minimum.</param>
    /// <param name="Categories">Category names and identities captured when the proposal was created.</param>
    public sealed record ProposalMetadata(
        MapProposalId Id,
        PlayerResponses.Player? Author,
        DateTimeOffset CreatedAt,
        DateTimeOffset SubmittedAt,
        Rating Rating,
        double? MinAccuracy,
        CategorySnapshot[] Categories);

    /// <summary>
    /// A persistent ranking thread in one context, with at most one current ranked map.
    /// </summary>
    /// <param name="Id">Thread identity, retained across proposals and ranked-map revisions.</param>
    /// <param name="ContextId">Context to which the thread permanently belongs.</param>
    /// <param name="AuthorId">Original thread creator. Absent for imports or when the player was deleted.</param>
    /// <param name="CreatedAt">Thread creation time, not the waiting-time origin used to sort the queue.</param>
    /// <param name="RankedMapId">Current ranked map, absent before acceptance or after that map is deleted.</param>
    /// <param name="BoostCount">Number of active player boosts, not historical boosts or proposal votes.</param>
    /// <param name="IsBoostedByMe">
    /// Whether the authenticated viewer boosts this thread. False for anonymous viewers.
    /// </param>
    /// <param name="IsLegacyImport">Whether the thread belongs to the legacy-import synchronization path.</param>
    /// <param name="Proposals">
    /// Visible history and the viewer's own private drafts. Ordered by creation time, then proposal ID, ascending.
    /// Not by acceptance time. Queue results use ThreadPreview instead.
    /// </param>
    public sealed record Thread(
        MapRequestThreadId Id,
        ContextId ContextId,
        PlayerId? AuthorId,
        DateTimeOffset CreatedAt,
        RankedMapId? RankedMapId,
        int BoostCount,
        bool IsBoostedByMe,
        bool IsLegacyImport,
        Proposal[] Proposals);

    /// <summary>
    /// Complete proposed definition retained independently of the current ranked map.
    /// </summary>
    /// <param name="Requirements">Score eligibility requirements that acceptance applies to the ranked map.</param>
    /// <param name="Rating">Proposed ratings and whether they were calculated automatically during submission.</param>
    /// <param name="MapVersions">Resolved song difficulties and play modes, in ascending display order.</param>
    /// <param name="Categories">Explicit category associations, with names captured when the draft was created.</param>
    /// <param name="Achievements">
    /// Explicit ranked-map-list associations, not dynamically matched star achievements.
    /// </param>
    /// <param name="Comment">Optional original author comment, up to 2000 characters.</param>
    public sealed record Definition(
        RankedMapRequests.RankedMapRequirements Requirements,
        Rating Rating,
        MapPreview[] MapVersions,
        CategorySnapshot[] Categories,
        AchievementSnapshot[] Achievements,
#if NET10_0_OR_GREATER
        [property: MaxLength(2000)]
#endif
        string? Comment);

    /// <summary>
    /// Your per-context proposal usage and the current configurable rules. Counts are proposals, not threads.
    /// </summary>
    /// <param name="Drafts">Your private drafts in this context.</param>
    /// <param name="MaxDrafts">Maximum private drafts you may hold in this context.</param>
    /// <param name="RankingRequests">Your submitted proposals in threads without a current ranked map.</param>
    /// <param name="MaxRankingRequests">Maximum submitted new-ranking proposals you may hold in this context.</param>
    /// <param name="RevisionRequests">Your submitted proposals in threads with a current ranked map.</param>
    /// <param name="MaxRevisionRequests">Maximum submitted revision proposals you may hold in this context.</param>
    /// <param name="MinimumAmendmentTier">
    /// Minimum subscription tier for competing with another author's submitted proposal: 0 allows everyone,
    /// 1/2/3 require Tier1/Tier2/Tier3 respectively. Ranking-team members, guild leaders and managers bypass this rule.
    /// </param>
    /// <param name="MinimumPositiveVotes">
    /// Minimum positive ranking-team votes for acceptance, except for leader/manager bypass.
    /// </param>
    /// <param name="RequireNoNegativeVotes">
    /// Whether any negative ranking-team vote blocks acceptance, except for leader/manager bypass.
    /// </param>
    /// <param name="PriorityBonusPerBoost">
    /// Fractional waiting-time bonus per boost for Priority sorting: priority = waiting seconds * (1 + boosts * bonus).
    /// Waiting starts at the earliest currently submitted proposal. For example, 0.25 adds 25% per boost.
    /// </param>
    public sealed record Allowance(
        int Drafts,
        int MaxDrafts,
        int RankingRequests,
        int MaxRankingRequests,
        int RevisionRequests,
        int MaxRevisionRequests,
        int MinimumAmendmentTier,
        int MinimumPositiveVotes,
        bool RequireNoNegativeVotes,
        double PriorityBonusPerBoost);

    /// <summary>
    /// Rating snapshot, resolved before a draft can be submitted.
    /// </summary>
    /// <param name="AccStar">Accuracy stars. Absent on a draft when calculation is requested at submission.</param>
    /// <param name="DiffStar">Difficulty stars. Absent on a draft when calculation is requested at submission.</param>
    /// <param name="IsAccStarAutoCalculated">
    /// True once accuracy stars were calculated at submission. False for manual or unresolved ratings.
    /// </param>
    /// <param name="IsDiffStarAutoCalculated">
    /// True once difficulty stars were calculated at submission. False for manual or unresolved ratings.
    /// </param>
    public sealed record Rating(
        float? AccStar,
        float? DiffStar,
        bool IsAccStarAutoCalculated,
        bool IsDiffStarAutoCalculated);

    /// <summary>
    /// Historical category association, retained even if the live category is renamed or deleted.
    /// </summary>
    /// <param name="Id">Original category ID. Does not guarantee that the category still exists.</param>
    /// <param name="Name">Category name captured when the draft was created.</param>
    public sealed record CategorySnapshot(CategoryId Id, string Name);

    /// <summary>
    /// Historical ranked-map-list association, retained even if the live achievement is renamed or deleted.
    /// </summary>
    /// <param name="Id">Original achievement ID. Does not guarantee that the achievement still exists.</param>
    /// <param name="Name">Achievement name captured when the draft was created.</param>
    public sealed record AchievementSnapshot(int Id, string Name);

    /// <summary>
    /// Complete definition with a lifecycle discriminator: Draft, Submitted, Accepted or Closed.
    /// There is no edit-proposal subtype. A thread with a current ranked map applies accepted proposals as revisions.
    /// </summary>
    /// <param name="Id">Proposal identity, retained throughout its lifecycle.</param>
    /// <param name="ThreadId">Thread containing this proposal.</param>
    /// <param name="AuthorId">Proposal author. Absent for imports or when the player was deleted.</param>
    /// <param name="Author">Proposal author profile. Absent for imports or when the player was deleted.</param>
    /// <param name="CreatedAt">Draft creation time, not submission or acceptance time.</param>
    /// <param name="Definition">Entire desired definition, not a patch against an earlier proposal.</param>
    /// <param name="Votes">
    /// Community and ranking-team votes. These cannot be changed once the proposal is accepted or closed.
    /// </param>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(Draft), "Draft")]
    [JsonDerivedType(typeof(Submitted), "Submitted")]
    [JsonDerivedType(typeof(Accepted), "Accepted")]
    [JsonDerivedType(typeof(Closed), "Closed")]
    public closed record Proposal(
        MapProposalId Id,
        MapRequestThreadId ThreadId,
        PlayerId? AuthorId,
        PlayerResponses.Player? Author,
        DateTimeOffset CreatedAt,
        Definition Definition,
        Vote[] Votes)
    {
        /// <summary>
        /// Private draft visible only to its author. Revising it creates a replacement and deletes this draft atomically.
        /// </summary>
        public sealed record Draft(
            MapProposalId Id,
            MapRequestThreadId ThreadId,
            PlayerId? AuthorId,
            PlayerResponses.Player? Author,
            DateTimeOffset CreatedAt,
            Definition Definition,
            Vote[] Votes)
            : Proposal(Id, ThreadId, AuthorId, Author, CreatedAt, Definition, Votes);

        /// <summary>
        /// Published, immutable definition awaiting explicit review. Votes may change while it is submitted.
        /// </summary>
        public sealed record Submitted(
            MapProposalId Id,
            MapRequestThreadId ThreadId,
            PlayerId? AuthorId,
            PlayerResponses.Player? Author,
            DateTimeOffset CreatedAt,
            Definition Definition,
            Vote[] Votes,
            DateTimeOffset SubmittedAt)
            : Proposal(Id, ThreadId, AuthorId, Author, CreatedAt, Definition, Votes);

        /// <summary>
        /// Historical definition applied to the ranked map at 'acceptedAt'. Order accepted proposals by 'acceptedAt'
        /// to reconstruct revisions. An older accepted proposal need not match the current ranked map.
        /// The 'acceptedById' field is absent for imports or a deleted reviewer. Accepted proposals cannot be applied again.
        /// </summary>
        public sealed record Accepted(
            MapProposalId Id,
            MapRequestThreadId ThreadId,
            PlayerId? AuthorId,
            PlayerResponses.Player? Author,
            DateTimeOffset CreatedAt,
            Definition Definition,
            Vote[] Votes,
            DateTimeOffset SubmittedAt,
            DateTimeOffset AcceptedAt,
            PlayerId? AcceptedById)
            : Proposal(Id, ThreadId, AuthorId, Author, CreatedAt, Definition, Votes);

        /// <summary>
        /// Proposal withdrawn by its author, denied by a reviewer or superseded by another accepted proposal.
        /// Definition and votes remain in history. It cannot be reopened. A missing 'submittedAt' means it was never
        /// published and remains private. The 'closedById' field may be absent for imports or a deleted player.
        /// The reason is optional.
        /// </summary>
        public sealed record Closed(
            MapProposalId Id,
            MapRequestThreadId ThreadId,
            PlayerId? AuthorId,
            PlayerResponses.Player? Author,
            DateTimeOffset CreatedAt,
            Definition Definition,
            Vote[] Votes,
            DateTimeOffset? SubmittedAt,
            DateTimeOffset ClosedAt,
            PlayerId? ClosedById,
            string? Reason)
            : Proposal(Id, ThreadId, AuthorId, Author, CreatedAt, Definition, Votes);
    }

    /// <summary>
    /// A community or ranking-team vote. One player may hold one of each kind on the same proposal.
    /// </summary>
    /// <param name="PlayerId">Player who cast the vote.</param>
    /// <param name="IsPositive">True for support, false for opposition.</param>
    /// <param name="VotedAt">Time the vote was last cast or replaced, not a history of earlier choices.</param>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(Community), "Community")]
    [JsonDerivedType(typeof(RankingTeam), "RankingTeam")]
    public closed record Vote(PlayerId PlayerId, bool IsPositive, DateTimeOffset VotedAt)
    {
        /// <summary>
        /// Community feedback. Contributes to CommunityVotes queue sorting, not the acceptance gate.
        /// </summary>
        public sealed record Community(PlayerId PlayerId, bool IsPositive, DateTimeOffset VotedAt)
            : Vote(PlayerId, IsPositive, VotedAt);

        /// <summary>
        /// Vote counted towards the acceptance gate, with an optional explanation for support or opposition.
        /// </summary>
        public sealed record RankingTeam(PlayerId PlayerId, bool IsPositive, DateTimeOffset VotedAt, string? Reason)
            : Vote(PlayerId, IsPositive, VotedAt);
    }

    /// <summary>
    /// A shared song difficulty found in another thread in the same context.
    /// </summary>
    /// <param name="ThreadId">Thread containing the possible duplicate.</param>
    /// <param name="RankedMapId">Matching ranked map. Absent when this match is a submitted proposal.</param>
    /// <param name="ProposalId">Matching submitted proposal. Absent when this match is a ranked map.</param>
    public sealed record DuplicateMatch(
        MapRequestThreadId ThreadId,
        RankedMapId? RankedMapId,
        MapProposalId? ProposalId);

    /// <summary>
    /// One active boost allocation. No boost history is retained after release or transfer.
    /// </summary>
    /// <param name="ThreadId">Thread currently receiving your boost.</param>
    /// <param name="ContextId">Thread's context, used to locate it or release the boost.</param>
    public sealed record BoostAllocation(MapRequestThreadId ThreadId, ContextId ContextId);

    /// <summary>
    /// Global boost allowance shared across every guild and context.
    /// </summary>
    /// <param name="Limit">Maximum active boosts for your current subscription tier and server configuration.</param>
    /// <param name="Allocations">
    /// Current allocations. May exceed the limit after a tier downgrade. Release excess boosts.
    /// </param>
    public sealed record BoostAllowance(int Limit, BoostAllocation[] Allocations);

    /// <summary>
    /// Successful explicit acceptance. Competing submitted proposals are closed and thread boosts released.
    /// </summary>
    /// <param name="Proposal">Accepted definition snapshot, including its acceptance timestamp and reviewer.</param>
    /// <param name="RankedMapId">Ranked map created or updated in the proposal's thread.</param>
    public sealed record Acceptance(Proposal.Accepted Proposal, RankedMapId RankedMapId);

    /// <summary>
    /// Problem details retained by the client, including actionable duplicate and quota information.
    /// </summary>
    /// <param name="Status">HTTP status code of the failed request.</param>
    /// <param name="Title">Short description of the failure.</param>
    /// <param name="Detail">Explanation of this particular failure.</param>
    /// <param name="Code">Machine-readable failure code, when provided by the API.</param>
    /// <param name="Errors">Validation failures grouped by field.</param>
    /// <param name="Duplicates">Possible duplicates that require explicit acknowledgement before submission.</param>
    /// <param name="Limit">Maximum permitted usage when an allowance is exhausted.</param>
    /// <param name="Current">Current usage, when included in an allowance failure.</param>
    /// <param name="PositiveVotes">Positive ranking-team votes counted when acceptance was refused.</param>
    /// <param name="NegativeVotes">Negative ranking-team votes counted when acceptance was refused.</param>
    /// <param name="RequiredPositiveVotes">Minimum positive ranking-team votes required for acceptance.</param>
    /// <param name="RequireNoNegativeVotes">Whether negative ranking-team votes block acceptance.</param>
    /// <param name="RetryAfterSeconds">Delay before retrying when an upstream service rate limits the request.</param>
    public sealed record Error(
        int? Status,
        string? Title,
        string? Detail,
        string? Code = null,
        Dictionary<string, string[]>? Errors = null,
        DuplicateMatch[]? Duplicates = null,
        int? Limit = null,
        int? Current = null,
        int? PositiveVotes = null,
        int? NegativeVotes = null,
        int? RequiredPositiveVotes = null,
        bool? RequireNoNegativeVotes = null,
        double? RetryAfterSeconds = null);
}