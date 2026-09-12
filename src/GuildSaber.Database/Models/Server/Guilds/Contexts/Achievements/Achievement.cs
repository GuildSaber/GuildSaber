using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Achievements;

public closed class Achievement(
    Achievement.AchievementId id,
    GuildId guildId,
    ContextId contextId,
    CategoryId? categoryId,
    AchievementInfo info,
    AchievementDiscordBindings discordBindings,
    uint? progressionOrder,
    bool isLocking,
    Xp unlockXp)
{
    public AchievementId Id { get; init; } = id;
    public GuildId GuildId { get; init; } = guildId;
    public ContextId ContextId { get; init; } = contextId;
    public CategoryId? CategoryId { get; init; } = categoryId;

    public AchievementInfo Info { get; set; } = info;
    public AchievementDiscordBindings DiscordBindings { get; set; } = discordBindings;

    public uint? ProgressionOrder { get; set; } = progressionOrder;
    public bool IsLocking { get; set; } = isLocking;
    public Xp UnlockXp { get; set; } = unlockXp;

    public Guild Guild { get; init; } = null!;
    public Context Context { get; init; } = null!;
    public Category? Category { get; init; }

    [JsonConverter(typeof(AchievementIdJsonConverter))]
    public readonly record struct AchievementId(int Value) : IEFStrongTypedId<AchievementId, int>
    {
        public static bool TryParse(string? from, out AchievementId value)
        {
            if (int.TryParse(from, out var id))
            {
                value = new AchievementId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator int(AchievementId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
}

public enum AchievementType : byte
{
    AccStar,
    DiffStar,
    RankedMapList
}

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.HasDiscriminator<AchievementType>("type")
            .HasValue<AccStarAchievement>(AchievementType.AccStar)
            .HasValue<DiffStarAchievement>(AchievementType.DiffStar)
            .HasValue<RankedMapListAchievement>(AchievementType.RankedMapList)
            .IsComplete();

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.ContextId, x.CategoryId });

        builder.Property(x => x.Id)
            .HasGenericConversion<Achievement.AchievementId, int>()
            .ValueGeneratedOnAdd();

        builder.ComplexProperty(x => x.Info).Configure(new AchievementInfoConfiguration());
        builder.ComplexProperty(x => x.DiscordBindings).Configure(new AchievementDiscordBindingsConfiguration());

        builder.Property(x => x.UnlockXp)
            .HasConversion<float>(from => from, to => FromPersistence(to));

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Achievements_UnlockXp_Range",
                $"\"UnlockXp\" BETWEEN " +
                $"{Xp.MinValue.ToString(CultureInfo.InvariantCulture)} AND " +
                Xp.MaxValue.ToString("R", CultureInfo.InvariantCulture));
            tableBuilder.HasCheckConstraint(
                "CK_Achievements_StarRange",
                "\"MaxStar\" IS NULL OR \"MinStar\" < \"MaxStar\"");
        });

        builder.HasOne(x => x.Guild)
            .WithMany()
            .HasForeignKey(x => x.GuildId);

        builder.HasOne(x => x.Context)
            .WithMany(x => x.Achievements)
            .HasForeignKey(x => x.ContextId);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }

    private static Xp FromPersistence(float value)
    {
        var result = Xp.TryCreate(value);
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Invalid persisted Xp value '{value}': {result.Error}");
    }
}

public class AchievementIdJsonConverter : JsonConverter<Achievement.AchievementId>
{
    public override Achievement.AchievementId Read(ref Utf8JsonReader reader, Type typeToConvert,
                                                   JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Number
            ? new Achievement.AchievementId(reader.GetInt32())
            : throw new JsonException("Cannot convert to AchievementId");

    public override void Write(Utf8JsonWriter writer, Achievement.AchievementId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}