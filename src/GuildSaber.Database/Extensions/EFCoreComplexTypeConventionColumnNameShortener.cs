using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace GuildSaber.Database.Extensions;

/// <summary>
/// Ensure the name in the database don't overflow the max identifier length of the database.
/// EF Core have some funky behaviors, with its migrations identifying changes when there are none, when there is truncated
/// names.
/// This convention will rename the columns of complex types to a shorter version composed like so:
/// "ComplexEntityTypeName_PropertyName".
/// If the new naming short is still too long, it wont do anything for those properties. (EF Core's default truncated
/// naming should take over).
/// </summary>
/// <remarks>
/// In your DbContext, override the ConfigureConventions method:
/// <code>
/// protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
/// {
///     base.ConfigureConventions(configurationBuilder);
///     configurationBuilder.Conventions.Remove&lt;ComplexTypeAttributeConvention&gt;();
///     configurationBuilder.Conventions.Add(services => new EFCoreComplexTypeConventionColumnNameShortener(
///         services.GetRequiredService{ProviderConventionSetBuilderDependencies}())
///     );
/// }
/// </code>
/// </remarks>
/// <param name="dependencies"></param>
public class EFCoreComplexTypeConventionColumnNameShortener(ProviderConventionSetBuilderDependencies dependencies)
    : ComplexTypeAttributeConvention(dependencies)
{
    public override void ProcessComplexPropertyAdded(
        IConventionComplexPropertyBuilder propertyBuilder,
        IConventionContext<IConventionComplexPropertyBuilder> context)
    {
        base.ProcessComplexPropertyAdded(propertyBuilder, context);
        foreach (var prop in propertyBuilder.Metadata.ComplexType.GetProperties())
        {
            const int limitAfterEFCoreShouldAutoTruncates = 64;

            var newName = $"{propertyBuilder.Metadata.Name}_{prop.Name}";
            if (newName.Length >= limitAfterEFCoreShouldAutoTruncates)
                continue;

            prop.SetColumnName($"{propertyBuilder.Metadata.Name}_{prop.Name}");
        }
    }
}