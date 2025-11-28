using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public class LevelRequirement
{
    public required ELevelRequirementType Type { get; set; }

    //TODO: Feature - Add Xp based levels (post-release)
    //public int? MinXp { get; set; }
    public RankedMapRating.AccuracyStar? MinAccStar { get; set; }
    public RankedMapRating.DifficultyStar? MinDiffStar { get; set; }
    public uint MinPassCount { get; set; }

    public enum ELevelRequirementType
    {
        //Xp = 1,
        DiffStar = 2,
        AccStar = 3
    }
}

public class LevelRequirementConfiguration : IComplexPropertyConfiguration<LevelRequirement>
{
    public ComplexPropertyBuilder<LevelRequirement> Configure(ComplexPropertyBuilder<LevelRequirement> builder)
    {
        builder.Property(x => x.MinAccStar)
            .HasConversion<float?>(from => from.HasValue ? from.Value.Value : null,
                to => to.HasValue ? new RankedMapRating.AccuracyStar(to.Value) : null);
        builder.Property(x => x.MinDiffStar)
            .HasConversion<float?>(from => from.HasValue ? from.Value.Value : null,
                to => to.HasValue ? new RankedMapRating.DifficultyStar(to.Value) : null);

        return builder;
    }
}