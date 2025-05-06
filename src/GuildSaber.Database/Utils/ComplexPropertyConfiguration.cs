using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Utils;

public interface IComplexPropertyConfiguration<T>
{
    public ComplexPropertyBuilder<T> Configure(ComplexPropertyBuilder<T> builder);
}

public static class ComplexPropertyConfigurationExtensions
{
    public static ComplexPropertyBuilder<T> Configure<T>(
        this ComplexPropertyBuilder<T> self, IComplexPropertyConfiguration<T> builder)
        => builder.Configure(self);
}