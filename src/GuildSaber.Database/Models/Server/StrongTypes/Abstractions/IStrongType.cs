namespace GuildSaber.Database.Models.Server.StrongTypes.Abstractions;

public interface IStrongType<T>
{
    T Value { get; init; }
}