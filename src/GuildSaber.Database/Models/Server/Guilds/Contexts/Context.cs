using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildId = GuildSaber.Database.Models.Server.Guilds.Guild.GuildId;

namespace GuildSaber.Database.Models.Server.Guilds;

public class Context
{
    public ContextId Id { get; init; }
    public GuildId GuildId { get; init; }

    public EContextType Type { get; init; }

    public ContextInfo Info { get; set; }
    //TODO: Add settings for context, like if it only takes up new scores, etc.

    public IList<Point> Points { get; init; } = null!;
    public IList<Level> Levels { get; init; } = null!;
    public IList<RankedMap> RankedMaps { get; init; } = null!;
    public IList<Member> Members { get; init; } = null!;
    public IList<ContextMember> ContextMembers { get; init; } = null!;

    [JsonConverter(typeof(ContextIdJsonConverter))]
    public readonly record struct ContextId(int Value) : IEFStrongTypedId<ContextId, int>
    {
        public static bool TryParse(string? from, out ContextId value)
        {
            if (int.TryParse(from, out var id))
            {
                value = new ContextId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator int(ContextId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }

    /// <summary>
    /// Maybe this will end up being a type union (from inheritance), but it will fit for now.
    /// </summary>
    public enum EContextType
    {
        Default = 0,
        Tournament = 1 << 0,
        Temporary = 1 << 1
    }
}

public class ContextConfiguration : IEntityTypeConfiguration<Context>
{
    public void Configure(EntityTypeBuilder<Context> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasGenericConversion<Context.ContextId, int>()
            .ValueGeneratedOnAdd();
        builder.ComplexProperty(x => x.Info).Configure(new ContextInfoConfiguration());

        builder.HasOne<Guild>()
            .WithMany(x => x.Contexts).HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Points)
            .WithMany();
        builder.HasMany(x => x.Levels)
            .WithOne().HasForeignKey(x => x.ContextId);
        builder.HasMany(x => x.RankedMaps)
            .WithOne().HasForeignKey(x => x.ContextId);
        builder.HasMany(x => x.Members)
            .WithMany(x => x.Contexts)
            .UsingEntity<ContextMember>();
    }
}

public class ContextIdJsonConverter : JsonConverter<Context.ContextId>
{
    public override Context.ContextId Read(
        ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Number
            ? new Context.ContextId(reader.GetInt32())
            : throw new JsonException("Cannot convert to ContextId");

    public override void Write(
        Utf8JsonWriter writer, Context.ContextId value,
        JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}