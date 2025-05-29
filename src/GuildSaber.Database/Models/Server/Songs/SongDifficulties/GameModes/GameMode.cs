using CSharpFunctionalExtensions;
using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;

public class GameMode
{
    public GameModeId Id { get; init; }
    public required string Name { get; set; }

    public readonly record struct GameModeId(ulong Value) : IEFStrongTypedId<GameModeId, ulong>
    {
        public static Result<GameModeId> TryCreate(ulong? value) => value switch
        {
            null => Failure<GameModeId>("GameMode ID must not be null."),
            0 => Failure<GameModeId>("GameMode ID must not be 0."),
            _ => Success(new GameModeId(value.Value))
        };
    }
}

public class GameModeConfiguration : IEntityTypeConfiguration<GameMode>
{
    public void Configure(EntityTypeBuilder<GameMode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<GameMode.GameModeId, ulong>();
        builder.Property(x => x.Name).HasMaxLength(128);
    }
}