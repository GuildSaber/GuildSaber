﻿using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildId = GuildSaber.Database.Models.Server.Guilds.Guild.GuildId;

namespace GuildSaber.Database.Models.Server.Guilds;

public class GuildContext
{
    public GuildContextId Id { get; init; }
    public GuildId GuildId { get; init; }

    public ContextType Type { get; init; }
    public GuildContextInfo Info { get; set; }

    public IList<Point> Points { get; init; } = null!;

    public readonly record struct GuildContextId(uint Value) : IEFStrongTypedId<GuildContextId, uint>
    {
        public static bool TryParse(string from, out GuildContextId value)
        {
            if (uint.TryParse(from, out var id))
            {
                value = new GuildContextId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator uint(GuildContextId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }

    /// <summary>
    /// Maybe this will end up being a type union (from inheritance), but it will fit for now.
    /// </summary>
    public enum ContextType
    {
        Default = 0,
        Tournament = 1 << 0,
        Temporary = 1 << 1
    }
}

public class GuildContextConfiguration : IEntityTypeConfiguration<GuildContext>
{
    public void Configure(EntityTypeBuilder<GuildContext> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasGenericConversion<GuildContext.GuildContextId, uint>()
            .ValueGeneratedOnAdd();
        builder.ComplexProperty(x => x.Info).Configure(new GuildContextInfoConfiguration());

        builder.HasOne<Guild>()
            .WithMany(x => x.Contexts).HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Points)
            .WithMany();
    }
}