using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public abstract class Level
{
    public LevelId Id { get; init; }
    public Guild.GuildId GuildId { get; init; }
    public Context.ContextId ContextId { get; init; }
    public Category.CategoryId? CategoryId { get; init; }

    public required LevelInfo Info { get; set; }

    public required uint Order { get; set; }
    public required bool IsLocking { get; set; }

    public ELevelType Type { get; private init; }
    public enum ELevelType : byte { RankedMapList = 0, DiffStar = 1, AccStar = 2 }

    [JsonConverter(typeof(LevelIdJsonConverter))]
    public readonly record struct LevelId(int Value) : IEFStrongTypedId<LevelId, int>
    {
        public static bool TryParse(string? from, out LevelId value)
        {
            if (int.TryParse(from, out var id))
            {
                value = new LevelId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator int(LevelId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
}

public class LevelConfiguration : IEntityTypeConfiguration<Level>
{
    public void Configure(EntityTypeBuilder<Level> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.ContextId, x.CategoryId });
        builder.HasDiscriminator(x => x.Type)
            .HasValue<RankedMapListLevel>(Level.ELevelType.RankedMapList)
            .HasValue<DiffStarLevel>(Level.ELevelType.DiffStar)
            .HasValue<AccStarLevel>(Level.ELevelType.AccStar)
            .IsComplete();

        builder.Property(x => x.Id)
            .HasGenericConversion<Level.LevelId, int>()
            .ValueGeneratedOnAdd();

        builder.ComplexProperty(x => x.Info).Configure(new LevelInfoConfiguration());

        builder.HasOne<Guild>()
            .WithMany()
            .HasForeignKey(x => x.GuildId);

        builder.HasOne<Context>()
            .WithMany(x => x.Levels)
            .HasForeignKey(x => x.ContextId);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}

public class LevelIdJsonConverter : JsonConverter<Level.LevelId>
{
    public override Level.LevelId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Number
            ? new Level.LevelId(reader.GetInt32())
            : throw new JsonException("Cannot convert to LevelId");

    public override void Write(Utf8JsonWriter writer, Level.LevelId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}