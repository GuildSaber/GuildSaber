namespace GuildSaber.Database.Models.StrongTypes.Abstractions;

public interface IStrongType<T>
{
    T Value { get; init; }
}