using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Result;

public static class MaybeExtensions
{
    extension<T>(Task<Maybe<T>> self)
    {
        public async Task Match(Action<T> some, Action none)
            => (await self.ConfigureAwait(false)).Match(some, none);

        public async Task<K> Match<K>(Func<T, K> some, Func<K> none)
            => (await self.ConfigureAwait(false)).Match(some, none);
    }

    extension<T, E>(Task<Result<T, E>> resultTask)
    {
        /// <summary>
        /// Returns a new failure result Task if the predicate is false. Otherwise, returns the starting result Task.
        /// </summary>
        public async Task<Result<T, E>> Ensured(Func<T, bool> predicate, Func<T, E> errorPredicate)
            => (await resultTask.ConfigureAwait(false)).Ensure(predicate, errorPredicate);

        /// <summary>
        /// Returns a new failure result Task if the predicate is false. Otherwise returns the starting result Task.
        /// </summary>
        public async Task<Result<T, E>> Ensured(Func<T, bool> predicate, E error)
            => (await resultTask.ConfigureAwait(false)).Ensure(predicate, error);
    }
}