﻿using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Songs.SongDifficulties;

public class SongDifficulty
{
    public SongDifficultyId Id { get; init; }
    public BLLeaderboardId? BLLeaderboardId { get; init; }
    public GameMode.GameModeId GameModeId { get; init; }
    public Song.SongId SongId { get; init; }

    public GameMode GameMode { get; init; } = null!;
    public Song Song { get; init; } = null!;

    public readonly record struct SongDifficultyId(ulong Value) : IEFStrongTypedId<SongDifficultyId, ulong>
    {
        public static bool TryParse(string from, out SongDifficultyId value)
        {
            if (ulong.TryParse(from, out var id))
            {
                value = new SongDifficultyId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator ulong(SongDifficultyId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
}

public class SongDifficultyConfiguration : IEntityTypeConfiguration<SongDifficulty>
{
    public void Configure(EntityTypeBuilder<SongDifficulty> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasGenericConversion<SongDifficulty.SongDifficultyId, ulong>()
            .ValueGeneratedOnAdd();
        builder.Property(x => x.BLLeaderboardId)
            .HasConversion<string?>(from => from, to => BLLeaderboardId.CreateUnsafe(to));

        builder.HasOne(x => x.GameMode).WithMany().HasForeignKey(x => x.GameModeId);
        builder.HasOne(x => x.Song).WithMany(x => x.SongDifficulties).HasForeignKey(x => x.SongId);
    }
}