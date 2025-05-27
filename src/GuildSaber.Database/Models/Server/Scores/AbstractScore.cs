using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
using PlayerId = GuildSaber.Database.Models.Server.Players.Player.PlayerId;

namespace GuildSaber.Database.Models.Server.Scores;

public abstract record AbstractScore
{
    public ScoreId Id { get; init; }
    public PlayerId PlayerId { get; init; }
    public SongDifficultyId SongDifficultyId { get; init; }
    public EScoreType Type { get; init; }

    public uint BaseScore { get; init; }
    public EModifiers Modifiers { get; init; }
    public DateTimeOffset SetAt { get; init; }

    public uint MaxCombo { get; init; }
    public bool IsFullCombo { get; init; }
    public uint MissedNotes { get; init; }
    public uint BadCuts { get; init; }

    public PlayerHardwareInfo.EHMD HMD { get; set; }
    public enum EScoreType : byte { ScoreSaber = 0, BeatLeader = 1 }

    public readonly record struct ScoreId(ulong Value) : IEFStrongTypedId<ScoreId, ulong>;

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
        Unk = 1 << 30
    }
}

public class AbstractScoreConfiguration : IEntityTypeConfiguration<AbstractScore>
{
    public void Configure(EntityTypeBuilder<AbstractScore> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<AbstractScore.ScoreId, ulong>();
        builder.HasDiscriminator(x => x.Type)
            .HasValue<ScoreSaberScore>(AbstractScore.EScoreType.ScoreSaber)
            .HasValue<BeatLeaderScore>(AbstractScore.EScoreType.BeatLeader)
            .IsComplete();

        builder.HasOne<Player>()
            .WithMany().HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SongDifficulty>()
            .WithMany().HasForeignKey(x => x.SongDifficultyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}