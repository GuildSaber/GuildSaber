using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Extensions;

public interface IComplexPropertyConfiguration<T>
{
    public ComplexPropertyBuilder<T> Configure(ComplexPropertyBuilder<T> builder);
}

public static class EFCoreComplexPropertyConfigurationExtensions
{
    public static ComplexPropertyBuilder<T> Configure<T>(
        this ComplexPropertyBuilder<T> self, IComplexPropertyConfiguration<T> builder)
        => builder.Configure(self);
}