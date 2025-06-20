using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Result;

public static class MaybeExtensions
{
    public static async Task Match<T>(this Task<Maybe<T>> self, Action<T> some, Action none)
        => (await self.ConfigureAwait(false)).Match(some, none);

    public static async Task<K> Match<T, K>(this Task<Maybe<T>> self, Func<T, K> some, Func<K> none)
        => (await self.ConfigureAwait(false)).Match(some, none);


    /// <summary>
    /// Returns a new failure result Task if the predicate is false. Otherwise returns the starting result Task.
    /// </summary>
    public static async Task<Result<T, E>> Ensured<T, E>(
        this Task<Result<T, E>> resultTask,
        Func<T, bool> predicate,
        Func<T, E> errorPredicate)
        => (await resultTask.ConfigureAwait(false)).Ensure(predicate, errorPredicate);

    /// <summary>
    /// Returns a new failure result Task if the predicate is false. Otherwise returns the starting result Task.
    /// </summary>
    public static async Task<Result<T, E>> Ensured<T, E>(
        this Task<Result<T, E>> resultTask,
        Func<T, bool> predicate,
        E error)
        => (await resultTask.ConfigureAwait(false)).Ensure(predicate, error);
}