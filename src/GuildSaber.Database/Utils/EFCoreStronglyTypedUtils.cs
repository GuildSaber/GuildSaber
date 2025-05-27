using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Utils;

public interface IEFStrongTypedId<T, TValue> where TValue : struct where T : struct, IEFStrongTypedId<T, TValue>
{
    TValue Value { get; init; }
}

public static class EFCoreStronglyTypedUtils
{
    /// <summary>
    /// Configures a property of a strongly typed ID to use a value converter.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property. This type must implement the IStronglyTypedId interface.</typeparam>
    /// <typeparam name="TType">The type of the ID value in the strongly typed ID.</typeparam>
    /// <param name="propertyBuilder">The builder being used to configure the property.</param>
    /// <returns>
    /// The same builder instance so that multiple configuration calls can be chained.
    /// </returns>
    /// <remarks>
    /// This method sets up a conversion that will convert between the ID value in the typed ID (TType) and the actual
    /// property type (TProperty) when reading from and writing to the database.
    /// </remarks>
    public static PropertyBuilder<TProperty> HasGenericConversion<TProperty, TType>(
        this PropertyBuilder<TProperty> propertyBuilder)
        where TProperty : struct, IEFStrongTypedId<TProperty, TType>
        where TType : struct
        => propertyBuilder.HasConversion(
            v => v.Value,
            v => new TProperty { Value = v }
        );
}