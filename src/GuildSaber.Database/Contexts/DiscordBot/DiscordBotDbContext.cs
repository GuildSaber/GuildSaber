using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.DiscordBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GuildSaber.Database.Contexts.DiscordBot;

public sealed class DiscordBotDbContext : DbContext
{
    public DiscordBotDbContext(DbContextOptions<DiscordBotDbContext> options) : base(options) { }
    public DiscordBotDbContext() { }


    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Remove<ComplexTypeAttributeConvention>();
        configurationBuilder.Conventions.Add(services => new EFCoreComplexTypeConventionColumnNameShortener(
            services.GetRequiredService<ProviderConventionSetBuilderDependencies>())
        );
    }
}