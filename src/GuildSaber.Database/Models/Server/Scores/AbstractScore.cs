using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;

namespace GuildSaber.Database.Models.Server.Scores;

public abstract class AbstractScore
{
    public ScoreId Id { get; set; }
    public required PlayerId PlayerId { get; init; }
    public required SongDifficultyId SongDifficultyId { get; init; }

    public required BaseScore BaseScore { get; init; }
    public required EModifiers Modifiers { get; init; }
    public required DateTimeOffset SetAt { get; init; }

    public required int? MaxCombo { get; init; }
    public required bool IsFullCombo { get; init; }
    public required int MissedNotes { get; init; }
    public required int BadCuts { get; init; }
    public required PlayerHardwareInfo.EHMD HMD { get; set; }

    public EScoreType Type { get; private init; }
    public enum EScoreType : byte { ScoreSaber = 0, BeatLeader = 1 }

    [Flags]
    public enum EModifiers
    {
        None = 0,
        NoObstacles = 1 << 0,
        NoBombs = 1 << 1,
        NoFail = 1 << 2,
        SlowerSong = 1 << 3,
        BatteryEnergy = 1 << 4,
        InstaFail = 1 << 5,
        SmallNotes = 1 << 6,
        ProMode = 1 << 7,
        FasterSong = 1 << 8,
        StrictAngles = 1 << 9,
        DisappearingArrows = 1 << 10,
        GhostNotes = 1 << 11,
        NoArrows = 1 << 12,
        SuperFastSong = 1 << 13,
        OldDots = 1 << 14,
        OffPlatform = 1 << 15,
        Unk = 1 << 30,

        /*TODO: Assert that Req EModifiers.ProhibitedDefaults matches RankedMapRequest.EModifiers.ProhibitedDefaults
         * to prevent future mismatches.*/
        /// <summary>
        /// All modifiers that are commonly prohibited to giving points in ranked maps.
        /// Such as NoObstacles, NoBombs, NoFail, SlowerSong, NoArrows and OffPlatform.
        /// </summary>
        ProhibitedDefaults = NoObstacles | NoBombs | NoFail | SlowerSong | NoArrows | OffPlatform
    }
}

public class AbstractScoreConfiguration : IEntityTypeConfiguration<AbstractScore>
{
    public void Configure(EntityTypeBuilder<AbstractScore> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new ScoreId(to))
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => x.SetAt);

        builder.HasDiscriminator(x => x.Type)
            .HasValue<ScoreSaberScore>(AbstractScore.EScoreType.ScoreSaber)
            .HasValue<BeatLeaderScore>(AbstractScore.EScoreType.BeatLeader)
            .IsComplete();

        builder.Property(x => x.BaseScore)
            .HasConversion<int>(from => from, to => BaseScore.CreateUnsafe(to).Value);

        builder.HasOne<Player>()
            .WithMany().HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SongDifficulty>()
            .WithMany().HasForeignKey(x => x.SongDifficultyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}