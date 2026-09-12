using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.DiscordBot.FlexHistories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GuildSaber.Database.Contexts.DiscordBot;

public sealed class DiscordBotDbContext : DbContext
{
    public DiscordBotDbContext(DbContextOptions<DiscordBotDbContext> options) : base(options) { }
    public DiscordBotDbContext() { }

    public DbSet<FlexHistory> FlexHistories { get; set; } = null!;
    public DbSet<FlexHistoryAchievementStat> FlexHistoryAchievementStats { get; set; } = null!;
    public DbSet<FlexHistoryPointStat> FlexHistoryPointStats { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FlexHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new FlexHistoryAchievementStatConfiguration());
        modelBuilder.ApplyConfiguration(new FlexHistoryPointStatConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Remove<ComplexTypeAttributeConvention>();
        configurationBuilder.Conventions.Add(services => new EFCoreComplexTypeConventionColumnNameShortener(
            services.GetRequiredService<ProviderConventionSetBuilderDependencies>())
        );
    }
}