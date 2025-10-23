using System.ComponentModel;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildId = GuildSaber.Database.Models.Server.Guilds.Guild.GuildId;
using PlayerId = GuildSaber.Database.Models.Server.Players.Player.PlayerId;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
using ScoreId = GuildSaber.Database.Models.Server.Scores.AbstractScore.ScoreId;
using PointId = GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId;

namespace GuildSaber.Database.Models.Server.RankedScores;

public class RankedScore : IComparable<RankedScore>
{
    public RankedScoreId Id { get; init; }

    public required GuildId GuildId { get; init; }
    public required GuildContext.GuildContextId ContextId { get; init; }
    public required RankedMap.RankedMapId RankedMapId { get; init; }
    public required SongDifficultyId SongDifficultyId { get; init; }
    public required PointId PointId { get; init; }
    public required PlayerId PlayerId { get; init; }

    public required ScoreId ScoreId { get; set; }
    public required ScoreId? PrevScoreId { get; set; }

    public required EState State { get; set; }
    public required EDenyReason DenyReason { get; set; }
    public required EffectiveScore EffectiveScore { get; set; }
    public required RawPoints RawPoints { get; set; }

    public required int Rank { get; set; }
    /* Date won't be stored here, it can just be based on the underlying score's SetAt property.
     (Because the ranked map and rules can be tweaked, reassigning dates here would be confusing) */

    /// <remarks>
    /// Old piece of code non-tested and used for the sake of getting things to work.
    /// </remarks>
    public int CompareTo(RankedScore? other) => other switch
    {
        // Surely this score is better than a non-existing one.
        null => 1,
        { State: var otherState } => ((State & EState.NonPointGiving) == 0) switch
        {
            // State & otherState don't have any non-allowed flags.
            true when (otherState & EState.NonPointGiving) == 0 =>
                RawPoints.CompareTo(other.RawPoints) switch
                {
                    0 => EffectiveScore.CompareTo(other.EffectiveScore) switch
                    {
                        0 => ScoreId.Value.CompareTo(other.ScoreId.Value),
                        var x => x
                    },
                    var x => x
                },
            // Other state has non-allowed flag(s) when State doesn't.
            true => 1,
            // Other state doesn't have non-allowed flag(s) while State has.
            false when (otherState & EState.NonPointGiving) == 0 => -1,
            // State and Other state have non-allowed flag(s).
            false => RawPoints.CompareTo(other.RawPoints) switch
            {
                0 => EffectiveScore.CompareTo(other.EffectiveScore) switch
                {
                    0 => ScoreId.Value.CompareTo(other.ScoreId.Value),
                    var x => x
                },
                var x => x
            }
        }
    };

    public readonly record struct RankedScoreId(long Value) : IEFStrongTypedId<RankedScoreId, long>
    {
        public static bool TryParse(string from, out RankedScoreId value)
        {
            if (long.TryParse(from, out var id))
            {
                value = new RankedScoreId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator long(RankedScoreId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }

    /// <summary>
    /// The state of the ranked score in the ranking process.
    /// </summary>
    /// <remarks>
    /// All states but Approved are non-point-giving states.
    /// </remarks>
    [Flags]
    public enum EState
    {
        [Description("Score is in no particular state. It might be unprocessed, it won't give points.")]
        None = 0,

        [Description("Score has been selected to giving points.")]
        Selected = 1 << 0,

        [Description("Score has been denied from giving points. Check DenyReason for clues.")]
        Denied = 1 << 1,

        [Description("Score has been removed from ranking. (Player removed from guild, or score was invalidated)")]
        Removed = 1 << 2,

        [Description("Score is awaiting review by a scoring team member.")]
        Pending = 1 << 3,

        [Description("Score has been confirmed by scoring team member.")]
        Confirmed = 1 << 4,

        [Description("Score has been refused by a scoring team member.")]
        Refused = 1 << 5,

        //Note for future me: Should auto-confirmed be its own state? Or just Confirmed 

        NonPointGiving = None | Denied | Removed | Pending | Refused | Confirmed
    }

    /// <summary>
    /// The reason(s) a score was denied.
    /// </summary>
    [Flags]
    public enum EDenyReason
    {
        [Description("No reason specified.")]
        Unspecified = 0,

        [Description("Score did not meet the minimum score requirement.")]
        MinAccuracyRequirements = 1 << 0,

        [Description("Score used prohibited modifiers.")]
        ProhibitedModifiers = 1 << 1,

        [Description("Score was missing required modifiers.")]
        MissingModifiers = 1 << 2,

        [Description("Score had too much pause time.")]
        TooMuchPaused = 1 << 3,

        [Description("Score was not a full combo when one was required.")]
        NoFullCombo = 1 << 4,

        [Description("Score was missing trackers.")]
        MissingTrackers = 1 << 5
    }
}

public class RankedScoreConfiguration : IEntityTypeConfiguration<RankedScore>
{
    public void Configure(EntityTypeBuilder<RankedScore> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasGenericConversion<RankedScore.RankedScoreId, long>()
            .ValueGeneratedOnAdd();
        builder.Property(x => x.EffectiveScore)
            .HasConversion<int>(from => from, to => EffectiveScore.CreateUnsafe(to).Value);
        builder.Property(x => x.RawPoints)
            .HasConversion<float>(from => from, to => RawPoints.CreateUnsafe(to).Value);

        builder.HasOne<Guild>()
            .WithMany(x => x.RankedScores).HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<GuildContext>()
            .WithMany().HasForeignKey(x => x.ContextId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RankedMap>()
            .WithMany().HasForeignKey(x => x.RankedMapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SongDifficulty>()
            .WithMany().HasForeignKey(x => x.SongDifficultyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany().HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AbstractScore>()
            .WithMany().HasForeignKey(x => x.ScoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AbstractScore>()
            .WithMany().HasForeignKey(x => x.PrevScoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Point>()
            .WithMany().HasForeignKey(x => x.PointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}