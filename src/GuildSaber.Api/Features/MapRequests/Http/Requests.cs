#if NET10_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
#endif
using System.Text.Json.Serialization;
using GuildSaber.Api.Features.RankedMaps.Http;

namespace GuildSaber.Api.Features.MapRequests.Http;

public static class MapProposalRequests
{
    /// <summary>
    /// Filters for querying ranking requests and proposals.
    /// </summary>
    /// <param name="Search">
    /// A search term applied to the proposed songs' names, authors, mappers and BeatSaver keys.
    /// A song hash or !bsr key searches that exact identity. Queue searches only consider submitted proposals.
    /// Matching threads still include all their submitted proposals.
    /// </param>
    /// <param name="IsRevision">
    /// If true, only returns requests for threads with a current ranked map. If false, only returns new-ranking
    /// requests. If omitted, returns both. This reflects the thread's current state, not its state at proposal creation.
    /// </param>
    public record struct Filters(
        [property: FromQuery(Name = "search")] string? Search = null,
        [property: FromQuery(Name = "isRevision")] bool? IsRevision = null
    );

    /// <summary>
    /// Duplicate acknowledgement when publishing a draft.
    /// </summary>
    /// <param name="Force">
    /// If true, allows submission despite difficulties shared with ranked maps or submitted proposals in other
    /// threads in the same context. If false, any duplicate match returns 409 with code 'duplicate_maps'
    /// and the matching thread/map/proposal IDs in 'duplicates'. The matches should be reviewed before retrying with true.
    /// Only applies to threads without a current ranked map. Does not bypass ownership, permissions, quotas,
    /// validation, rating calculation or acceptance vote requirements.
    /// </param>
    public record struct SubmissionOptions(
        [property: FromQuery(Name = "force")] bool Force = false
    );

    /// <summary>
    /// Available sorting methods for the ranking queue. Ordering is calculated at request time.
    /// </summary>
    public enum EMapRequestSorter
    {
        /// <summary>Waiting seconds * (1 + boost count * configured bonus). Descending shows highest priority first.</summary>
        Priority = 0,

        /// <summary>Earliest currently submitted proposal's submission time. Ascending shows oldest requests first.</summary>
        WaitingSince = 1,

        /// <summary>Number of active thread boosts. Ties show oldest waiting requests first.</summary>
        BoostCount = 2,

        /// <summary>Positive minus negative community votes on submitted proposals. Ties show oldest requests first.</summary>
        CommunityVotes = 3
    }

    public enum EMapProposalSorter
    {
        CreatedAt = 0,
        Id = 1
    }

    /// <summary>
    /// Withdraw or deny a submitted proposal without deleting its history.
    /// </summary>
    /// <param name="Reason">Optional explanation retained with the closed proposal.</param>
    public sealed record Close(string? Reason);

    /// <summary>
    /// Transfer an existing boost to the thread identified by the route.
    /// </summary>
    /// <param name="SourceThreadId">
    /// Thread currently holding your boost. It may belong to another context or guild.
    /// Moving the boost does not consume another allowance slot. The target must have a submitted proposal.
    /// </param>
    public sealed record MoveBoost([property: JsonRequired] MapRequestThreadId SourceThreadId);

    /// <summary>
    /// A community vote on a submitted proposal. Does not count towards the ranking-team votes required for acceptance.
    /// </summary>
    /// <param name="IsPositive">
    /// True to support the proposal, false to oppose it. Replaces your existing community vote.
    /// </param>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record CommunityVote([property: JsonRequired] bool IsPositive);

    /// <summary>
    /// A ranking-team vote on a submitted proposal. Meeting the vote requirements does not auto-accept it.
    /// </summary>
    /// <param name="IsPositive">
    /// True to support acceptance, false to oppose it. Replaces your existing ranking-team vote.
    /// </param>
    /// <param name="Reason">
    /// Optional explanation for either a positive or a negative vote, retained in proposal history.
    /// </param>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record RankingTeamVote([property: JsonRequired] bool IsPositive, string? Reason);

    /// <summary>
    /// Complete proposed configuration, not a patch. Creates a private draft without publishing it to the queue.
    /// Drafts cannot be edited: delete and recreate them, or withdraw submitted proposals and create another variant.
    /// </summary>
    /// <param name="MapVersions">
    /// At least one map version. Each resolved song difficulty and play-mode combination must be unique.
    /// Song versions and leaderboard IDs are resolved during draft creation: a BeatLeader leaderboard is required,
    /// but a missing ScoreSaber leaderboard does not prevent creation.
    /// </param>
    /// <param name="Requirements">Complete score requirements. Send an empty object to use their defaults.</param>
    /// <param name="Rating">
    /// Required rating object. Either star rating may be null or omitted to calculate it when the draft is submitted.
    /// Calculation failure prevents submission. No zero-value fallback is used.
    /// </param>
    /// <param name="CategoryIds">
    /// Distinct category IDs from the thread's guild. Required even when empty. An empty array proposes no categories.
    /// </param>
    /// <param name="AchievementIds">
    /// Distinct ranked-map-list achievement IDs from the thread's context. Required even when empty.
    /// Star-based achievements are matched dynamically and must not be supplied here.
    /// </param>
    /// <param name="Comment">Optional author comment retained with the proposal, up to 2000 characters.</param>
    public sealed record CreateDraft(
        [property: JsonRequired] MapVersionInput[] MapVersions,
        [property: JsonRequired] RankedMapRequests.RankedMapRequirements Requirements,
        [property: JsonRequired] RankedMapRequests.ManualRating Rating,
        [property: JsonRequired] CategoryId[] CategoryIds,
        [property: JsonRequired] int[] AchievementIds,
#if NET10_0_OR_GREATER
        [property: MaxLength(2000)]
#endif
        string? Comment = null
    );

    /// <summary>
    /// A song difficulty and play mode to include in the proposed ranked map.
    /// </summary>
    /// <param name="Song">
    /// Select a song by BeatSaver key or an exact version hash, using the type discriminator.
    /// </param>
    /// <param name="Characteristic">Supported BeatSaver characteristic name, such as Standard.</param>
    /// <param name="Difficulty">Difficulty within the selected song version and characteristic.</param>
    /// <param name="PlayMode">Supported GuildSaber play-mode name, not a leaderboard ID.</param>
    /// <param name="Order">
    /// Ascending display order. The lowest-order difficulty supplies automatic ratings at submission.
    /// When either rating is automatic, different difficulties must not tie for the lowest order.
    /// </param>
    public sealed record MapVersionInput(
        [property: JsonRequired] SongReference Song,
        [property: JsonRequired] string Characteristic,
        [property: JsonRequired] EDifficulty Difficulty,
        [property: JsonRequired] string PlayMode,
        [property: JsonRequired] byte Order
    );

    /// <summary>
    /// Song selector: use type "Key" with key to resolve BeatSaver's last-listed version during draft creation,
    /// or type "Hash" with hash to select an exact version. The resolved version is retained in the proposal.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ByKey), "Key")]
    [JsonDerivedType(typeof(ByHash), "Hash")]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public closed record SongReference
    {
        /// <summary>
        /// Resolve a BeatSaver key using the last entry in BeatSaver's versions array.
        /// </summary>
        /// <param name="Key">
        /// BeatSaver map key. Resolution happens at draft creation, not again at submission or acceptance.
        /// </param>
        public sealed record ByKey([property: JsonRequired] BeatSaverKey Key) : SongReference;

        /// <summary>
        /// Select an exact song version, including an older version.
        /// </summary>
        /// <param name="Hash">Song hash identifying the exact BeatSaver version to use.</param>
        public sealed record ByHash([property: JsonRequired] SongHash Hash) : SongReference;
    }
}