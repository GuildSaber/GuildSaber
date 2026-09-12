using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
using PointId = GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId;

namespace GuildSaber.Database.Models.Server.RankedScores;

public abstract class RankedScore : IComparable<RankedScore>
{
    public RankedScoreId Id { get; init; }

    public required GuildId GuildId { get; init; }
    public required ContextId ContextId { get; init; }
    public required RankedMapId RankedMapId { get; init; }
    public required SongDifficultyId SongDifficultyId { get; init; }
    public required PointId PointId { get; init; }
    public required PlayerId PlayerId { get; init; }

    public required ScoreId ScoreId { get; set; }
    public required ScoreId? PrevScoreId { get; set; }

    public required bool IsSelected { get; set; }
    public required EffectiveScore EffectiveScore { get; set; }
    public required DateTimeOffset EditedAt { get; set; }

    public AbstractScore Score { get; set; } = null!;
    public AbstractScore? PrevScore { get; init; }
    public Player Player { get; init; } = null!;
    public RankedMap RankedMap { get; init; } = null!;
    public SongDifficulty SongDifficulty { get; init; } = null!;

    public ERankedScoreType Type { get; private init; }

    public enum ERankedScoreType : byte { Valid = 0, Invalid = 1, Pending = 2, Accepted = 3, Refused = 4 }

    public int CompareTo(RankedScore? other) => other switch
    {
        // Surely this score is better than a non-existing one.
        null => 1,
        // The other score is scored so we can compare it's state and points.
        ScoredRankedScore otherRankedScore => this switch
        {
            // We make sure to prioritize scores that are inclined to giving points.
            ScoredRankedScore self => IsGivingPointOrPending().CompareTo(other.IsGivingPointOrPending()) switch
            {
                0 => self.RawPoints.CompareTo(otherRankedScore.RawPoints) switch
                {
                    0 => EffectiveScore.CompareTo(otherRankedScore.EffectiveScore) switch
                    {
                        0 => PreferBlScore(otherRankedScore),
                        var effectiveScoreComparison => effectiveScoreComparison
                    },
                    var rawPointsComparison => rawPointsComparison
                },
                var selectedComparison => selectedComparison
            },
            // Our score isn't scored, therefore it's worse than any scored.
            _ => -1
        },
        // The other score isn't scored.
        _ => this switch
        {
            // Our score is scored, therefore it's better than a non-scored one.
            ScoredRankedScore => 1,
            // Both scores aren't scored, we can simply compare their effective score.
            _ => EffectiveScore.CompareTo(other.EffectiveScore) switch
            {
                // We make sure to prioritize scores that are inclined to giving points.
                0 => IsGivingPointOrPending().CompareTo(other.IsGivingPointOrPending()) switch
                {
                    0 => PreferBlScore(other),
                    var x => x
                },
                var x => x
            }
        }
    };

    /// <summary>
    /// Compares this score with another score by preferring BeatLeader as an underlying score preference.
    /// If both scores are BeatLeader scores, it compares their BeatLeaderScoreId, if they exist, otherwise it falls back to
    /// comparing their ScoreId.
    /// </summary>
    /// <returns>
    /// 1 if this score should be preferred over the other, -1 if the other should be preferred, 0 if they are equal in terms
    /// of preference.
    /// </returns>
    private int PreferBlScore(RankedScore other) => Score switch
    {
        { Type: AbstractScore.EScoreType.BeatLeader } => other.Score switch
        {
            { Type: AbstractScore.EScoreType.BeatLeader } => (((BeatLeaderScore)Score).BeatLeaderScoreId,
                    ((BeatLeaderScore)other.Score).BeatLeaderScoreId) switch
                {
                    (null, null) => ScoreId.Value.CompareTo(other.ScoreId.Value),
                    (not null, null) => 1,
                    (null, not null) => -1,
                    ({ } blId, { } otherBlId) => ((int)blId).CompareTo(otherBlId)
                },
            _ => 1
        },
        _ => other.Score switch
        {
            { Type: AbstractScore.EScoreType.BeatLeader } => -1,
            _ => ScoreId.Value.CompareTo(other.ScoreId.Value)
        }
    };

    private bool IsGivingPointOrPending() => this is PointGivingRankedScore or PendingRankedScore;
}

public class RankedScoreConfiguration : IEntityTypeConfiguration<RankedScore>
{
    public void Configure(EntityTypeBuilder<RankedScore> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new RankedScoreId(to))
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.ContextId, x.PointId, x.RankedMapId });
        builder.HasIndex(x => new { x.RankedMapId, x.PlayerId, x.IsSelected });
        builder.HasIndex(x => new { x.PlayerId, x.IsSelected });
        builder.HasIndex(x => x.IsSelected);
        builder.HasIndex(x => x.EditedAt);

        builder.HasDiscriminator(x => x.Type)
            .HasValue<ValidRankedScore>(RankedScore.ERankedScoreType.Valid)
            .HasValue<InvalidRankedScore>(RankedScore.ERankedScoreType.Invalid)
            .HasValue<PendingRankedScore>(RankedScore.ERankedScoreType.Pending)
            .HasValue<AcceptedRankedScore>(RankedScore.ERankedScoreType.Accepted)
            .HasValue<RefusedRankedScore>(RankedScore.ERankedScoreType.Refused)
            .IsComplete();

        builder.Property(x => x.EffectiveScore)
            .HasConversion<int>(from => from, to => EffectiveScore.CreateUnsafe(to).Value);

        builder.HasOne<Guild>()
            .WithMany(x => x.RankedScores).HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Context>()
            .WithMany().HasForeignKey(x => x.ContextId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RankedMap)
            .WithMany(x => x.RankedScores).HasForeignKey(x => x.RankedMapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SongDifficulty)
            .WithMany().HasForeignKey(x => x.SongDifficultyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Player)
            .WithMany().HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Score)
            .WithMany().HasForeignKey(x => x.ScoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PrevScore)
            .WithMany().HasForeignKey(x => x.PrevScoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Point>()
            .WithMany().HasForeignKey(x => x.PointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}