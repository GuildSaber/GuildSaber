using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct UuidV7
{
    private readonly Guid _value;

    private UuidV7(Guid value)
        => _value = value;

    public static UuidV7 Create() => new(Guid.CreateVersion7());

    public static UuidV7 Create(DateTimeOffset timestamp) => new(Guid.CreateVersion7(timestamp));

    public static Result<UuidV7> TryParse(string? value)
        => Guid.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<UuidV7>("Invalid UuidV7 format. Must be a valid GUID string.");

    public static Result<UuidV7> TryCreate(Guid value)
        => value.Version == 7
            ? Success(new UuidV7(value))
            : Failure<UuidV7>("Invalid Guid format. Must be a version 7.");

    public static UuidV7? CreateUnsafe(Guid? value)
        => value is null || value.Value.Version != 7
            ? null
            : new UuidV7(value.Value);

    public static implicit operator Guid(UuidV7 id)
        => id._value;

    public override string ToString()
        => _value.ToString();
}